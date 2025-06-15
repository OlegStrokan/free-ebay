export enum AI_TEMPLATES {
  BASIC_CHAT_TEMPLATE = `You are an expert software engineer, give concise response.
     User: {input}
     AI:`,
  CONTEXT_AWARE_CHAT_TEMPLATE = `You are an expert software engineer, give concise response.
    
     Current conversation:
     {chat_history}
     
     User: {input}
     AI:`,

  DOCUMENT_CONTEXT_CHAT_TEMPLATE = `Answer the question based only on the following context:
     {context}
     
     Question: {question}`,
}
