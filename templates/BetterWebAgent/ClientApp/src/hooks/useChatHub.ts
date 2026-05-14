import { useState, useEffect, useCallback, useRef } from "react"
import * as signalR from "@microsoft/signalr"

export interface Message {
  id: string
  source: "user" | "agent" | "system"
  content: string
  timestamp: Date
  isStreaming?: boolean
}

export interface ChatSession {
  id: string
  title: string
  createdAt: Date
  updatedAt: Date
}

interface ChatMessageDto {
  id: string
  chatSessionId: string
  source: string
  content: string
  createdAt: string
}

interface ChatSessionDto {
  id: string
  title: string
  createdAt: string
  updatedAt: string
}

function toMessage(dto: ChatMessageDto): Message {
  return {
    id: dto.id,
    source: dto.source as Message["source"],
    content: dto.content,
    timestamp: new Date(dto.createdAt),
  }
}

function toSession(dto: ChatSessionDto): ChatSession {
  return {
    id: dto.id,
    title: dto.title,
    createdAt: new Date(dto.createdAt),
    updatedAt: new Date(dto.updatedAt),
  }
}

export function useChatHub(hubUrl: string) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [isConnected, setIsConnected] = useState(false)
  const [sessions, setSessions] = useState<ChatSession[]>([])
  const [currentChatId, setCurrentChatId] = useState<string | null>(null)
  const [messages, setMessages] = useState<Message[]>([])
  const [isStreaming, setIsStreaming] = useState(false)

  // Refs let SignalR event callbacks see the latest values without re-binding handlers.
  const currentChatIdRef = useRef<string | null>(null)
  const streamingContentRef = useRef("")
  useEffect(() => { currentChatIdRef.current = currentChatId }, [currentChatId])

  // Establish connection.
  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build()

    setConnection(conn)
    return () => { conn.stop() }
  }, [hubUrl])

  // Wire up handlers + start.
  useEffect(() => {
    if (!connection) return

    connection.on("AllChats", (sessionDtos: ChatSessionDto[]) => {
      setSessions(sessionDtos.map(toSession))
    })

    connection.on("ChatCreated", (dto: ChatSessionDto) => {
      setSessions(prev => [toSession(dto), ...prev])
    })

    connection.on("ChatActivated", (chatId: string) => {
      // Update the ref synchronously so events arriving in the same tick
      // (ReceiveMessage / ReceiveStreamToken for this brand-new chat) pass
      // the chatId === currentChatIdRef.current guard below.
      currentChatIdRef.current = chatId
      setCurrentChatId(chatId)
    })

    connection.on("ChatUpdated", (dto: ChatSessionDto) => {
      setSessions(prev => prev.map(s => s.id === dto.id ? toSession(dto) : s))
    })

    connection.on("ChatDeleted", (chatId: string) => {
      setSessions(prev => prev.filter(s => s.id !== chatId))
      if (currentChatIdRef.current === chatId) {
        setCurrentChatId(null)
        setMessages([])
      }
    })

    connection.on("ChatHistory", (chatId: string, msgs: ChatMessageDto[]) => {
      if (chatId === currentChatIdRef.current) {
        setMessages(msgs.map(toMessage))
      }
    })

    connection.on("ChatCleared", (chatId: string) => {
      if (chatId === currentChatIdRef.current) setMessages([])
    })

    connection.on("ReceiveMessage", (chatId: string, dto: ChatMessageDto) => {
      if (chatId === currentChatIdRef.current || dto.source === "system") {
        setMessages(prev => [...prev, toMessage(dto)])
      }
    })

    connection.on("ReceiveStreamToken", (chatId: string, token: string) => {
      if (chatId !== currentChatIdRef.current) return

      streamingContentRef.current += token
      setIsStreaming(true)
      setMessages(prev => {
        const last = prev[prev.length - 1]
        if (last?.isStreaming) {
          return [...prev.slice(0, -1), { ...last, content: streamingContentRef.current }]
        }
        return [...prev, {
          id: crypto.randomUUID(),
          source: "agent",
          content: streamingContentRef.current,
          timestamp: new Date(),
          isStreaming: true,
        }]
      })
    })

    connection.on("StreamComplete", (chatId: string) => {
      if (chatId !== currentChatIdRef.current) return
      streamingContentRef.current = ""
      setIsStreaming(false)
      setMessages(prev => prev.map(m => m.isStreaming ? { ...m, isStreaming: false } : m))
    })

    connection.start()
      .then(() => setIsConnected(true))
      .catch(err => console.error("ChatHub connection error:", err))

    return () => {
      connection.off("AllChats")
      connection.off("ChatCreated")
      connection.off("ChatActivated")
      connection.off("ChatUpdated")
      connection.off("ChatDeleted")
      connection.off("ChatHistory")
      connection.off("ChatCleared")
      connection.off("ReceiveMessage")
      connection.off("ReceiveStreamToken")
      connection.off("StreamComplete")
    }
  }, [connection])

  const selectChat = useCallback(async (chatId: string | null) => {
    setCurrentChatId(chatId)
    setMessages([])
    streamingContentRef.current = ""
    setIsStreaming(false)
    if (chatId && connection?.state === signalR.HubConnectionState.Connected) {
      await connection.invoke("GetChatHistory", chatId)
    }
  }, [connection])

  const newChat = useCallback(() => {
    setCurrentChatId(null)
    setMessages([])
    streamingContentRef.current = ""
    setIsStreaming(false)
  }, [])

  const sendMessage = useCallback(async (text: string) => {
    if (!connection || !text.trim()) return
    streamingContentRef.current = ""
    const newChatId = await connection.invoke<string | null>("SendMessage", currentChatIdRef.current, text)
    // The server returns the chatId — adopt it (handles new session creation and /new returning null).
    setCurrentChatId(newChatId ?? null)
  }, [connection])

  const deleteChat = useCallback(async (chatId: string) => {
    if (!connection) return
    await connection.invoke("DeleteChat", chatId)
  }, [connection])

  return {
    isConnected,
    sessions,
    currentChatId,
    messages,
    isStreaming,
    selectChat,
    newChat,
    sendMessage,
    deleteChat,
  }
}
