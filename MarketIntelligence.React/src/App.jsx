import { useState, useRef, useEffect } from 'react'
import axios from 'axios'
import ReactMarkdown from 'react-markdown'
import './App.css'

function App() {
  // State for chat history, input, and loading status
  const [messages, setMessages] = useState([
    { role: 'assistant', content: 'Hello! I am your AI Financial Analyst. Ask me about stocks like MSFT or TSLA.' }
  ]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  
  // Generate a random Session ID once per browser refresh
  const [sessionId] = useState(() => Math.random().toString(36).substring(7));
  
  // Auto-scroll to bottom ref
  const messagesEndRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const sendMessage = async () => {
    if (!input.trim()) return;

    // 1. Update UI immediately with User message
    const userMessage = { role: 'user', content: input };
    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

    try {
      // 2. Call your Dockerized .NET API
      // Note: Ensure your Docker container is running on port 8080
      const response = await axios.post('http://localhost:8080/api/chat', {
          sessionId: sessionId,
          message: userMessage.content
      });

      // 3. Add AI Response
      const aiMessage = { role: 'assistant', content: response.data.response };
      setMessages(prev => [...prev, aiMessage]);
    } catch (error) {
      console.error(error);
      const errorMessage = { role: 'assistant', content: '⚠️ Error connecting to the API. Is Docker running?' };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter') sendMessage();
  };

  return (
    <div className="chat-container">
      <header className="chat-header">
        <h1>📈 Market Intelligence AI</h1>
      </header>

      <div className="messages-area">
        {messages.map((msg, index) => (
          <div key={index} className={`message-bubble ${msg.role}`}>
            <div className="message-content">
              {/* This renders the AI's bolding and lists properly */}
              <ReactMarkdown>{msg.content}</ReactMarkdown>
            </div>
          </div>
        ))}
        {isLoading && <div className="loading-indicator">Thinking...</div>}
        <div ref={messagesEndRef} />
      </div>

      <div className="input-area">
        <input 
          type="text" 
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyPress}
          placeholder="Ask about market trends..." 
          disabled={isLoading}
        />
        <button onClick={sendMessage} disabled={isLoading}>
          Send
        </button>
      </div>
    </div>
  )
}

export default App