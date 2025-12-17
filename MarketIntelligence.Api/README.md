# 📈 Market Intelligence AI Agent

> **An Autonomous "Quantamental" Financial Analyst that correlates macroeconomic trends with technical stock data.**

[![.NET](https://img.shields.io/badge/.NET-9.0-512bd4)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-blue)](https://reactjs.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ed)](https://www.docker.com/)
[![Azure AI](https://img.shields.io/badge/Azure-OpenAI-0078d4)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)

## 📖 Overview

Market Intelligence is an enterprise-grade AI platform designed to automate investment research. Unlike standard chatbots that hallucinate data, this agent uses a **RAG (Retrieval-Augmented Generation)** architecture to ground its answers in real-time data.

It operates in three autonomous stages:
1.  **Macro Scout:** Scrapes investment outlooks from major banks (Morgan Stanley, J.P. Morgan, BlackRock) using Headless Chrome.
2.  **Quant Screener:** Filters the US stock market for high-growth candidates (EPS/Sales growth > 20%) using Finviz data.
3.  **The Matchmaker:** Uses GPT-4o to semantically match "Bullish Themes" (e.g., AI Infrastructure) with specific growth stocks (e.g., NVDA, VST).

## 🏗 Architecture

The system is built as a **Microservices Architecture** using **Polyglot Persistence** (SQL + Vector).

```mermaid
graph TD
    User([👤 User / React App]) -->|HTTPS/JSON| API[🧠 Market API]

    subgraph "Docker Container Network"
        API -->|Semantic Kernel| Agent[🤖 AI Agent]
        
        Agent -->|SQL Plugin| SQL[(SQL Database)]
        Agent -->|Vector Plugin| Vector[(Azure AI Search)]
        
        Ingest[Worker Service] -->|Scrape & ETL| SQL
        Ingest -->|Embeddings| Vector
    end

    Ingest -.->|Puppeteer| Web[🌐 Finviz & Bank Reports]
    API -.->|LLM| OpenAI[☁️ Azure OpenAI]