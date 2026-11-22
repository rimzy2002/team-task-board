import { addDoc, collection, getFirestore, onSnapshot, orderBy, query, serverTimestamp } from 'firebase/firestore';
import { auth, firebaseApp } from '../config/firebase.js';

export const maxCommentLength = 1000;

function taskComments(uid, taskId) {
  if (auth.currentUser?.uid !== uid || !Number.isSafeInteger(taskId) || taskId <= 0) {
    throw new Error('Comments require a signed-in task owner.');
  }
  return collection(getFirestore(firebaseApp), 'users', uid, 'tasks', String(taskId), 'comments');
}

export function subscribeToTaskComments(uid, taskId, onChange, onError) {
  const ordered = query(taskComments(uid, taskId), orderBy('createdAt'));
  return onSnapshot(ordered, { includeMetadataChanges: true }, (snapshot) => {
    if (auth.currentUser?.uid !== uid) return;
    onChange({
      fromCache: snapshot.metadata.fromCache,
      comments: snapshot.docs.map((doc) => {
        const { text, createdAt } = doc.data();
        return { id: doc.id, text, createdAt: createdAt?.toDate?.(), pending: doc.metadata.hasPendingWrites };
      }).filter(({ text }) => typeof text === 'string'),
    });
  }, onError);
}

export async function postTaskComment(uid, taskId, value) {
  const text = value.trim();
  if (!text || text.length > maxCommentLength) throw new Error('Comment length is invalid.');
  await addDoc(taskComments(uid, taskId), { text, authorUid: auth.currentUser.uid, createdAt: serverTimestamp() });
}
