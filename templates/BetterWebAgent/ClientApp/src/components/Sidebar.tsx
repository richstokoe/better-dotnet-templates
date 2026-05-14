import { NavLink, useNavigate } from "react-router-dom"
import { MessageSquare, Bot, Plus, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Separator } from "@/components/ui/separator"
import { cn } from "@/lib/utils"
import type { ChatSession } from "@/hooks/useChatHub"

interface SidebarProps {
  sessions: ChatSession[]
  currentChatId: string | null
  isConnected: boolean
  onSelect: (chatId: string) => void
  onNew: () => void
  onDelete: (chatId: string) => void
}

export function Sidebar({ sessions, currentChatId, isConnected, onSelect, onNew, onDelete }: SidebarProps) {
  const navigate = useNavigate()

  const handleNew = () => {
    onNew()
    navigate("/")
  }

  return (
    <aside className="w-64 shrink-0 border-r border-border bg-card flex flex-col h-screen">
      <div className="p-3 flex items-center justify-between">
        <h1 className="text-sm font-semibold tracking-tight">BetterWebAgent</h1>
        <span className={cn(
          "h-2 w-2 rounded-full",
          isConnected ? "bg-green-500" : "bg-muted-foreground/40"
        )} title={isConnected ? "Connected" : "Disconnected"} />
      </div>

      <div className="px-3 pb-2">
        <Button onClick={handleNew} className="w-full" size="sm">
          <Plus className="size-4" />
          New conversation
        </Button>
      </div>

      <Separator />

      <nav className="p-2 space-y-1">
        <NavLink
          to="/"
          end
          className={({ isActive }) => cn(
            "flex items-center gap-2 rounded-md px-2 py-1.5 text-sm font-medium",
            isActive ? "bg-accent text-accent-foreground" : "hover:bg-accent/50"
          )}
        >
          <MessageSquare className="size-4" />
          Chats
        </NavLink>
        <NavLink
          to="/agents"
          className={({ isActive }) => cn(
            "flex items-center gap-2 rounded-md px-2 py-1.5 text-sm font-medium",
            isActive ? "bg-accent text-accent-foreground" : "hover:bg-accent/50"
          )}
        >
          <Bot className="size-4" />
          Agents
        </NavLink>
      </nav>

      <Separator />

      <div className="px-3 py-2 text-xs font-medium text-muted-foreground">
        Conversations
      </div>

      <ScrollArea className="flex-1">
        <div className="px-2 pb-2 space-y-1">
          {sessions.length === 0 && (
            <p className="px-2 py-1 text-xs text-muted-foreground">No conversations yet.</p>
          )}
          {sessions.map(s => (
            <div
              key={s.id}
              className={cn(
                "group flex items-center gap-1 rounded-md text-sm",
                s.id === currentChatId ? "bg-accent text-accent-foreground" : "hover:bg-accent/50"
              )}
            >
              <button
                onClick={() => { onSelect(s.id); navigate("/") }}
                className="flex-1 text-left truncate px-2 py-1.5"
                title={s.title}
              >
                {s.title}
              </button>
              <button
                onClick={() => onDelete(s.id)}
                className="opacity-0 group-hover:opacity-100 px-2 py-1.5 text-muted-foreground hover:text-destructive transition-opacity"
                title="Delete conversation"
              >
                <Trash2 className="size-3.5" />
              </button>
            </div>
          ))}
        </div>
      </ScrollArea>
    </aside>
  )
}
