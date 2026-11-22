import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { test } from 'node:test';
import { deleteApp, initializeApp } from 'firebase/app';
import { connectAuthEmulator, createUserWithEmailAndPassword, getAuth, signInWithEmailAndPassword } from 'firebase/auth';
import {
  addDoc, collection, connectFirestoreEmulator, deleteDoc, getDocsFromServer,
  getFirestore, onSnapshot, orderBy, query, serverTimestamp, updateDoc,
} from 'firebase/firestore';

test('Firestore isolates comment namespaces and broadcasts authorized writes', { timeout: 60000 }, async (t) => {
  function session(name) {
    const app = initializeApp({ projectId: 'demo-team-task-board', apiKey: 'demo-key' }, name);
    t.after(() => deleteApp(app));
    const auth = getAuth(app);
    const db = getFirestore(app);
    connectAuthEmulator(auth, 'http://127.0.0.1:9099', { disableWarnings: true });
    connectFirestoreEmulator(db, '127.0.0.1', 8080);
    return { auth, db };
  }
  const a = session('comment-owner');
  const a2 = session('comment-owner-second-session');
  const b = session('other-owner');
  const signedOut = session('signed-out');
  const email = `${randomUUID()}@example.test`;
  const password = randomUUID();
  const { user: { uid } } = await createUserWithEmailAndPassword(a.auth, email, password);
  const { user: { uid: otherUid } } = await createUserWithEmailAndPassword(b.auth, `other-${email}`, password);
  await signInWithEmailAndPassword(a2.auth, email, password);

  const thread = ({ db }, ownerUid = uid) => collection(db, 'users', ownerUid, 'tasks', '1', 'comments');
  const comment = (text, authorUid = uid) => ({ text, authorUid, createdAt: serverTimestamp() });
  const reject = (action) => assert.rejects(action, { code: 'permission-denied' });
  const original = await addDoc(thread(a), comment('Written before subscribing'));
  const snapshots = [];
  let listenerError;
  const unsubscribe = onSnapshot(query(thread(a), orderBy('createdAt')), { includeMetadataChanges: true }, (snapshot) => {
    if (!snapshot.metadata.fromCache && !snapshot.metadata.hasPendingWrites) {
      snapshots.push(snapshot.docs.map((row) => row.data().text));
    }
  }, (error) => { listenerError = error; });
  t.after(unsubscribe);
  async function waitForSnapshot(size) {
    for (let attempt = 0; attempt < 60; attempt++) {
      if (listenerError) throw listenerError;
      if (snapshots.at(-1)?.length === size) return;
      await new Promise((resolve) => setTimeout(resolve, 100));
    }
    assert.fail('No authoritative Firestore snapshot arrived.');
  }
  await waitForSnapshot(1);
  await addDoc(thread(a2), comment('Written in a second session'));
  await waitForSnapshot(2);
  assert.deepEqual(snapshots.at(-1), ['Written before subscribing', 'Written in a second session']);
  unsubscribe();
  const count = snapshots.length;
  await addDoc(thread(a2), comment('Written after unsubscribe'));
  assert.equal((await getDocsFromServer(thread(a))).size, 3);
  assert.equal(snapshots.length, count);

  for (const client of [signedOut, b]) {
    await reject(() => getDocsFromServer(thread(client)));
    await reject(() => addDoc(thread(client), comment('Unauthorized', otherUid)));
  }
  await reject(() => getDocsFromServer(thread(a, otherUid)));
  await reject(() => addDoc(thread(a, otherUid), comment('Wrong namespace')));
  for (const invalid of [
    comment('  \n\t\u00a0 '), comment('x'.repeat(1001)), comment(123), comment('Spoofed', otherUid),
    { ...comment('Unexpected field'), extra: true }, { ...comment('Forged timestamp'), createdAt: new Date(0) },
    { text: 'Missing timestamps', authorUid: uid },
  ]) await reject(() => addDoc(thread(a), invalid));
  await reject(() => updateDoc(original, { text: 'Edit' }));
  await reject(() => deleteDoc(original));
  await reject(() => getDocsFromServer(collection(a.db, 'public')));
  assert.equal((await getDocsFromServer(thread(a))).size, 3);
});
