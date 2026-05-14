import { useState, useEffect, useCallback } from "react"
import * as signalR from "@microsoft/signalr"

export interface AgentTask {
  id: string
  title: string
  prompt: string
  status: "Pending" | "Running" | "Completed" | "Failed" | "Cancelled"
  output?: string
  createdAt: Date
  startedAt?: Date
  completedAt?: Date
  chatSessionId?: string
}

interface AgentTaskDto {
  id: string
  title: string
  prompt: string
  status: string
  output?: string
  createdAt: string
  startedAt?: string
  completedAt?: string
  chatSessionId?: string
}

function toTask(dto: AgentTaskDto): AgentTask {
  return {
    id: dto.id,
    title: dto.title,
    prompt: dto.prompt,
    status: dto.status as AgentTask["status"],
    output: dto.output,
    createdAt: new Date(dto.createdAt),
    startedAt: dto.startedAt ? new Date(dto.startedAt) : undefined,
    completedAt: dto.completedAt ? new Date(dto.completedAt) : undefined,
    chatSessionId: dto.chatSessionId,
  }
}

export function useAgentHub(hubUrl: string) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const [tasks, setTasks] = useState<AgentTask[]>([])

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build()

    setConnection(conn)
    return () => { conn.stop() }
  }, [hubUrl])

  useEffect(() => {
    if (!connection) return

    connection.on("AllTasks", (dtos: AgentTaskDto[]) => {
      setTasks(dtos.map(toTask))
    })

    connection.on("TaskCreated", (dto: AgentTaskDto) => {
      setTasks(prev => [toTask(dto), ...prev])
    })

    connection.on("TaskUpdated", (dto: AgentTaskDto) => {
      setTasks(prev => prev.map(t => t.id === dto.id ? toTask(dto) : t))
    })

    connection.on("TaskDeleted", (taskId: string) => {
      setTasks(prev => prev.filter(t => t.id !== taskId))
    })

    connection.start().catch(err => console.error("AgentHub connection error:", err))

    return () => {
      connection.off("AllTasks")
      connection.off("TaskCreated")
      connection.off("TaskUpdated")
      connection.off("TaskDeleted")
    }
  }, [connection])

  const stopTask = useCallback(async (taskId: string) => {
    if (connection) await connection.invoke("StopTask", taskId)
  }, [connection])

  const deleteTask = useCallback(async (taskId: string) => {
    if (connection) await connection.invoke("DeleteTask", taskId)
  }, [connection])

  const clearCompleted = useCallback(async () => {
    if (connection) await connection.invoke("ClearCompletedTasks")
  }, [connection])

  return { tasks, stopTask, deleteTask, clearCompleted }
}
