import { useState, useRef, useEffect, type FormEvent } from "react"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import rehypeHighlight from "rehype-highlight"
import { Send, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ScrollArea } from "@/components/ui/scroll-area"
import { cn } from "@/lib/utils"
import { ThinkingBlock, ToolCallBlock, parseThinkingSegments } from "@/components/ThinkingBlock"
import type { Message } from "@/hooks/useChatHub"

// Centralised markdown renderer so the agent and system bubbles use the same
// plugin list. remark-gfm = GitHub Flavoured Markdown (tables, strikethrough,
// task lists, autolinks). rehype-highlight = syntax-highlighted code fences
// using highlight.js, styled by the github-dark theme imported in index.css.
function Markdown({ children }: { children: string }) {
  return (
    <ReactMarkdown
      remarkPlugins={[remarkGfm]}
      rehypePlugins={[[rehypeHighlight, { detect: true, ignoreMissing: true }]]}
    >
      {children}
    </ReactMarkdown>
  )
}

interface ChatProps {
  messages: Message[]
  isStreaming: boolean
  onSend: (text: string) => void
}

function MessageBubble({ message }: { message: Message }) {
  const isUser = message.source === "user"
  const isSystem = message.source === "system"

  if (isSystem) {
    return (
      <div className="w-full flex justify-center">
        <div className="text-xs text-muted-foreground italic max-w-2xl text-center">
          <Markdown>{message.content}</Markdown>
        </div>
      </div>
    )
  }

  if (isUser) {
    return (
      <div className="w-full flex justify-end">
        <div className="max-w-[80%] rounded-lg bg-primary text-primary-foreground px-4 py-2 text-sm whitespace-pre-wrap">
          {message.content}
        </div>
      </div>
    )
  }

  // Agent message — split into thinking + text segments and render appropriately.
  const segments = parseThinkingSegments(message.content)
  return (
    <div className="w-full flex justify-start">
      <div className="max-w-[80%] rounded-lg bg-muted px-4 py-2 text-sm">
        {segments.map((seg, i) => {
          if (seg.type === "think") {
            return <ThinkingBlock key={i} content={seg.content} inProgress={seg.inProgress ?? false} />
          }
          if (seg.type === "tool_call") {
            return <ToolCallBlock key={i} toolName={seg.toolName ?? "(tool)"} body={seg.content} inProgress={seg.inProgress ?? false} />
          }
          return seg.content && (
            <div key={i} className="prose prose-sm dark:prose-invert max-w-none [&>*:first-child]:mt-0 [&>*:last-child]:mb-0 [&_pre]:bg-zinc-900 [&_pre]:text-zinc-100 [&_pre]:rounded-md [&_pre]:p-3 [&_:not(pre)>code]:bg-muted-foreground/15 [&_:not(pre)>code]:px-1 [&_:not(pre)>code]:py-0.5 [&_:not(pre)>code]:rounded [&_:not(pre)>code]:text-[0.85em] [&_:not(pre)>code]:before:content-none [&_:not(pre)>code]:after:content-none">
              <Markdown>{seg.content}</Markdown>
            </div>
          )
        })}
        {message.isStreaming && (
          <span className="inline-block w-2 h-4 ml-1 bg-current animate-pulse" />
        )}
      </div>
    </div>
  )
}

export function Chat({ messages, isStreaming, onSend }: ChatProps) {
  const [input, setInput] = useState("")
  const scrollAnchorRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    scrollAnchorRef.current?.scrollIntoView({ behavior: "smooth" })
  }, [messages])

  useEffect(() => {
    if (!isStreaming) inputRef.current?.focus()
  }, [isStreaming])

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    if (!input.trim() || isStreaming) return
    onSend(input)
    setInput("")
  }

  return (
    <div className="flex flex-col h-screen flex-1">
      <ScrollArea className="flex-1 px-4">
        <div className="max-w-3xl mx-auto py-6 space-y-4">
          {messages.length === 0 && !isStreaming && (
            <div className="text-center text-muted-foreground py-12">
              <p className="text-sm">Start a conversation, or type <code className="px-1 py-0.5 rounded bg-muted text-xs">/help</code> to see slash commands.</p>
            </div>
          )}
          {messages.map(m => <MessageBubble key={m.id} message={m} />)}
          <div ref={scrollAnchorRef} />
        </div>
      </ScrollArea>

      <form onSubmit={handleSubmit} className="border-t border-border p-4">
        <div className="max-w-3xl mx-auto flex gap-2">
          <Input
            ref={inputRef}
            value={input}
            onChange={e => setInput(e.target.value)}
            placeholder="Message the agent… (try /help)"
            disabled={isStreaming}
            className={cn(isStreaming && "opacity-60")}
          />
          <Button type="submit" disabled={isStreaming || !input.trim()} size="icon">
            {isStreaming ? <Loader2 className="size-4 animate-spin" /> : <Send className="size-4" />}
          </Button>
        </div>
      </form>
    </div>
  )
}
