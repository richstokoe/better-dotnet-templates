import { Routes, Route } from "react-router-dom"
import { useChatHub } from "@/hooks/useChatHub"
import { useAgentHub } from "@/hooks/useAgentHub"
import { Sidebar } from "@/components/Sidebar"
import { Chat } from "@/components/Chat"
import { AgentsPage } from "@/components/AgentsPage"

export function App() {
  const chat = useChatHub("/hubs/chat")
  const agents = useAgentHub("/hubs/agents")

  return (
    <div className="flex h-screen bg-background text-foreground">
      <Sidebar
        sessions={chat.sessions}
        currentChatId={chat.currentChatId}
        isConnected={chat.isConnected}
        onSelect={chat.selectChat}
        onNew={chat.newChat}
        onDelete={chat.deleteChat}
      />
      <main className="flex-1 flex flex-col min-w-0">
        <Routes>
          <Route path="/" element={
            <Chat
              messages={chat.messages}
              isStreaming={chat.isStreaming}
              onSend={chat.sendMessage}
            />
          } />
          <Route path="/agents" element={
            <AgentsPage
              tasks={agents.tasks}
              onStop={agents.stopTask}
              onDelete={agents.deleteTask}
              onClearCompleted={agents.clearCompleted}
              onJumpToChat={chat.selectChat}
            />
          } />
        </Routes>
      </main>
    </div>
  )
}
