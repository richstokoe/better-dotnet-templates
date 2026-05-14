import { useState } from "react"
import { ChevronRight, Brain, Wrench } from "lucide-react"
import { cn } from "@/lib/utils"

interface ThinkingBlockProps {
  content: string
  /** True while the model is still inside a <think> block (no closing tag yet). */
  inProgress: boolean
}

/**
 * Collapsible chain-of-thought block. Expanded by default while streaming so
 * the user sees reasoning unfold, collapsed once the answer arrives.
 */
export function ThinkingBlock({ content, inProgress }: ThinkingBlockProps) {
  const [expanded, setExpanded] = useState(true)

  return (
    <div className="my-2 rounded-md border border-border bg-muted/30 text-xs">
      <button
        onClick={() => setExpanded(!expanded)}
        className="flex w-full items-center gap-1.5 px-2 py-1.5 text-muted-foreground hover:text-foreground transition-colors"
      >
        <ChevronRight className={cn("size-3.5 transition-transform", expanded && "rotate-90")} />
        <Brain className="size-3.5" />
        <span className="font-medium">
          {inProgress ? "Thinking…" : "Thought process"}
        </span>
      </button>
      {expanded && (
        <div className="px-3 pb-2 pt-1 border-t border-border whitespace-pre-wrap text-muted-foreground font-mono text-[0.7rem] leading-relaxed">
          {content}
        </div>
      )}
    </div>
  )
}

interface ToolCallBlockProps {
  /** Best-effort extracted tool name (e.g. "google_search.search"). */
  toolName: string
  /** Raw body between the markers. */
  body: string
  /** True while the model is still inside the tool-call block (no closing marker yet). */
  inProgress: boolean
}

/**
 * Renders a tool-call template that leaked into the response as plain text.
 *
 * This happens when the model emits its native tool-call format (Qwen-style
 * &lt;|tool_call&gt;...&lt;tool_call|&gt; markers, for example) instead of using the
 * structured `tool_calls` field that the OpenAI API surfaces and that
 * Microsoft.Agents.AI relies on for automatic execution.
 *
 * When you see this block, the tool DID NOT actually execute — the agent
 * framework never received a structured tool call. Either switch to a model
 * with native OpenAI-format tool calling, or enable LM Studio's tool-use
 * template for the model.
 */
export function ToolCallBlock({ toolName, body, inProgress }: ToolCallBlockProps) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div className="my-2 rounded-md border border-amber-300/60 dark:border-amber-900/60 bg-amber-50/50 dark:bg-amber-950/30 text-xs">
      <button
        onClick={() => setExpanded(!expanded)}
        className="flex w-full items-center gap-1.5 px-2 py-1.5 text-amber-900 dark:text-amber-200 hover:text-amber-950 dark:hover:text-amber-100 transition-colors"
      >
        <ChevronRight className={cn("size-3.5 transition-transform", expanded && "rotate-90")} />
        <Wrench className="size-3.5" />
        <span className="font-medium">
          {inProgress ? "Tool call…" : "Tool call"}: <code className="font-mono">{toolName}</code>
        </span>
      </button>
      {expanded && (
        <div className="px-3 pb-2 pt-1 border-t border-amber-300/40 dark:border-amber-900/40 whitespace-pre-wrap text-amber-800 dark:text-amber-300 font-mono text-[0.7rem] leading-relaxed">
          {body}
        </div>
      )}
    </div>
  )
}

/**
 * Split a streamed message into segments of normal text, &lt;think&gt; blocks, and
 * tool-call markers. The last segment may be in-progress while the model is
 * still generating.
 *
 * Recognised tool-call formats (all are leaked plaintext — the framework never
 * saw them as structured tool calls):
 *   - &lt;|tool_call&gt;...&lt;tool_call|&gt;   (Qwen / some Gemma variants)
 *   - &lt;tool_call&gt;...&lt;/tool_call&gt;     (Qwen 2.5 native template)
 */
export interface ParsedSegment {
  type: "text" | "think" | "tool_call"
  content: string
  inProgress?: boolean
  toolName?: string
}

export function parseThinkingSegments(text: string): ParsedSegment[] {
  const segments: ParsedSegment[] = []
  // Single combined regex so we walk the string in one pass and segments stay
  // in source order. Branches:
  //   group 1/2: <think>BODY</think>
  //   group 3/4: <|tool_call>BODY<tool_call|>   (asymmetric Qwen-style)
  //   group 5/6: <tool_call>BODY</tool_call>    (Qwen 2.5)
  const regex = /<think>([\s\S]*?)(<\/think>|$)|<\|tool_call>([\s\S]*?)(<tool_call\|>|$)|<tool_call>([\s\S]*?)(<\/tool_call>|$)/g
  let lastIndex = 0
  let match: RegExpExecArray | null

  while ((match = regex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      segments.push({ type: "text", content: text.slice(lastIndex, match.index) })
    }

    if (match[1] !== undefined) {
      const closed = match[2] === "</think>"
      segments.push({ type: "think", content: match[1], inProgress: !closed })
    } else if (match[3] !== undefined) {
      const closed = match[4] === "<tool_call|>"
      segments.push({ type: "tool_call", ...extractToolName(match[3]), inProgress: !closed })
    } else if (match[5] !== undefined) {
      const closed = match[6] === "</tool_call>"
      segments.push({ type: "tool_call", ...extractToolName(match[5]), inProgress: !closed })
    }

    lastIndex = regex.lastIndex
  }

  if (lastIndex < text.length) {
    segments.push({ type: "text", content: text.slice(lastIndex) })
  }

  return segments
}

/**
 * Pull a best-effort tool name out of the body. Handles the Qwen "call:NS:NAME{...}"
 * form, plain "NAME{...}", and JSON-shaped {"name":"NAME",...}. Falls back to "(tool)".
 */
function extractToolName(body: string): { content: string; toolName: string } {
  const trimmed = body.trim()

  // JSON-shaped: {"name":"foo", ...}
  const jsonNameMatch = trimmed.match(/^\s*\{[\s\S]*?"name"\s*:\s*"([^"]+)"/)
  if (jsonNameMatch) return { content: trimmed, toolName: jsonNameMatch[1] }

  // Colon-separated: "call:google_search:search{...}" → "google_search.search"
  // Or plain: "search{...}"
  const nameMatch = trimmed.match(/^(?:call:)?([\w:.\-]+?)\s*(?:\{|\(|$)/)
  if (nameMatch) return { content: trimmed, toolName: nameMatch[1].replace(/:/g, ".") }

  return { content: trimmed, toolName: "(tool)" }
}
