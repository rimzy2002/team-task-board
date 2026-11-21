import { createRoot } from 'react-dom/client';

function App() {
  return (
    <main>
      <h1>Team Task Board</h1>
      <p>React is running.</p>
    </main>
  );
}

createRoot(document.getElementById('root')).render(<App />);
