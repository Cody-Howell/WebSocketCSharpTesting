import { useEffect, useState } from 'react'
import './App.css'

function App() {
  const [messages, setMessages] = useState<{message: string, user: string, timestamp: string }[]>([]);
  const [myMessage, setMyMessage] = useState<string>("");
  const [socket, setSocket] = useState<WebSocket | null>(null);

  useEffect(() => {
    const ws = new WebSocket("ws://localhost:5000/ws"); // Point to your C# API
    setSocket(ws);

    ws.onopen = () => {
      console.log("Connected to WebSocket");
      ws.send("Hello from React!");
    };

    ws.onmessage = (event) => {
      console.log("Message from server:", event.data);
      setMessages((prev) => [...prev, JSON.parse(event.data)]);
    };

    ws.onclose = () => {
      console.log("WebSocket closed");
    };

    ws.onerror = (error) => {
      console.error("WebSocket error:", error);
    };

    return () => {
      ws.close();
    };
  }, []);

  const sendMessage = async () => {
    await fetch(`http://localhost:5000/chat/client?message=${myMessage}`);
  }

  const pingMessage = () => {
    socket?.send("Ping from React");
  };

  return (
    <div>
      <h1>React WebSocket Example</h1>
      <button onClick={pingMessage}>Send Ping</button>
      <ul>
        {messages.map((m, i) => (
          <li key={i}>{m.user}: {m.message} at {new Date(m.timestamp).toLocaleString()}</li>
        ))}
      </ul>
      <input type='text'
        value={myMessage}
        onChange={(e) => setMyMessage(e.target.value)} />

      <button onClick={sendMessage}>Send Message</button>

    </div>
  );
}

export default App
