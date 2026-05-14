import { useNavigate } from "react-router-dom"
import { Loader2, CheckCircle2, XCircle, Square, Trash2, ExternalLink, Clock } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ScrollArea } from "@/components/ui/scroll-area"
import { cn } from "@/lib/utils"
import type { AgentTask } from "@/hooks/useAgentHub"

interface AgentsPageProps {
  tasks: AgentTask[]
  onStop: (id: string) => void
  onDelete: (id: string) => void
  onClearCompleted: () => void
  onJumpToChat: (chatId: string) => void
}

function StatusIcon({ status }: { status: AgentTask["status"] }) {
  const className = "size-4 shrink-0"
  switch (status) {
    case "Pending":   return <Clock className={cn(className, "text-muted-foreground")} />
    case "Running":   return <Loader2 className={cn(className, "text-blue-500 animate-spin")} />
    case "Completed": return <CheckCircle2 className={cn(className, "text-green-500")} />
    case "Failed":    return <XCircle className={cn(className, "text-destructive")} />
    case "Cancelled": return <Square className={cn(className, "text-muted-foreground")} />
  }
}

export function AgentsPage({ tasks, onStop, onDelete, onClearCompleted, onJumpToChat }: AgentsPageProps) {
  const navigate = useNavigate()
  const hasCompleted = tasks.some(t => t.status !== "Pending" && t.status !== "Running")

  return (
    <div className="flex flex-col h-screen flex-1">
      <div className="border-b border-border px-6 py-4 flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">Agents</h2>
          <p className="text-xs text-muted-foreground">Long-running tasks promoted from chat conversations.</p>
        </div>
        {hasCompleted && (
          <Button variant="outline" size="sm" onClick={onClearCompleted}>
            Clear completed
          </Button>
        )}
      </div>

      <ScrollArea className="flex-1">
        <div className="max-w-3xl mx-auto py-6 px-6 space-y-3">
          {tasks.length === 0 && (
            <div className="text-center text-muted-foreground py-12">
              <p className="text-sm">No agent tasks yet. Ask a complex question in a chat and the agent will promote it here automatically.</p>
            </div>
          )}
          {tasks.map(task => (
            <div key={task.id} className="rounded-lg border border-border bg-card p-4">
              <div className="flex items-start gap-3">
                <StatusIcon status={task.status} />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <h3 className="font-medium text-sm truncate">{task.title}</h3>
                    <span className="text-xs text-muted-foreground shrink-0">{task.status}</span>
                  </div>
                  <p className="text-xs text-muted-foreground mt-1 line-clamp-2">{task.prompt}</p>
                </div>
                <div className="flex items-center gap-1 shrink-0">
                  {task.chatSessionId && (
                    <Button
                      variant="ghost" size="icon"
                      onClick={() => { onJumpToChat(task.chatSessionId!); navigate("/") }}
                      title="Open originating chat"
                    >
                      <ExternalLink className="size-4" />
                    </Button>
                  )}
                  {(task.status === "Pending" || task.status === "Running") && (
                    <Button variant="ghost" size="icon" onClick={() => onStop(task.id)} title="Stop">
                      <Square className="size-4" />
                    </Button>
                  )}
                  <Button variant="ghost" size="icon" onClick={() => onDelete(task.id)} title="Delete">
                    <Trash2 className="size-4 text-muted-foreground hover:text-destructive" />
                  </Button>
                </div>
              </div>
              {task.output && task.status === "Completed" && (
                <details className="mt-3 text-xs">
                  <summary className="cursor-pointer text-muted-foreground hover:text-foreground">Output</summary>
                  <pre className="mt-2 p-3 rounded bg-muted overflow-x-auto whitespace-pre-wrap">{task.output}</pre>
                </details>
              )}
            </div>
          ))}
        </div>
      </ScrollArea>
    </div>
  )
}
