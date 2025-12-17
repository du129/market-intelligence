# Market Intelligence AI Platform 📈

An Enterprise-grade AI Agent that correlates structured financial data (SQL) with unstructured news sentiment (Vector Search) to provide market analysis.

## 🏗 Architecture
This project implements a **Microservices Architecture** using **Polyglot Persistence**:

*   **Ingestion Service (Worker):** Background ETL process that scrapes market data and news.
*   **Inference Service (API):** ASP.NET Core Web API acting as the AI Brain.
*   **Frontend (UI):** React + Vite for real-time interaction.
*   **Infrastructure:** Fully Dockerized stack (SQL Server, API, Worker).

## 🚀 Tech Stack
*   **Core:** .NET 9, C#, React
*   **AI:** Microsoft Semantic Kernel, OpenAI GPT-4o, Azure AI Search (Vector Store)
*   **Data:** Azure SQL Edge (Docker), Entity Framework Core
*   **DevOps:** Docker Compose, Multi-stage Dockerfiles

## 🧠 How it Works (RAG + Agents)
1.  **Ingestion:** The Worker generates/scrapes stock data and embeds news articles into Vector space.
2.  **Reasoning:** The Agent receives a user query (e.g., *"Why did MSFT drop?"*).
3.  **Tool Execution:**
    *   Calls `MarketPlugin` (SQL) for price history.
    *   Calls `NewsPlugin` (Vector DB) for semantic context.
4.  **Synthesis:** The LLM correlates the price drop with specific news events (e.g., "Earnings miss").

## 🛠 How to Run
1.  Clone the repo.
2.  Update `appsettings.json` with Azure Credentials.
3.  Run:
    ```bash
    docker-compose up --build
    ```
4.  Open `http://localhost:5173` to chat with the Analyst.

graph TD
    User[React Frontend] -->|HTTP/JSON| API[ASP.NET Core API]
    
    subgraph "Docker Container Network"
        API -->|Semantic Kernel| Agent[AI Agent]
        
        Agent -->|SQL Plugin| DB[(SQL Server)]
        Agent -->|Vector Plugin| Vector[(Azure AI Search)]
        
        Ingest[Ingestor Worker] -->|EF Core| DB
        Ingest -->|Embeddings| Vector
    end
    
    API -->|LLM Reasoning| OpenAI[Azure OpenAI]