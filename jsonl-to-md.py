#!/usr/bin/env python3
import json, sys, pathlib

def extract_text(content):
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        parts = []
        for p in content:
            if isinstance(p, dict):
                if "text" in p and isinstance(p["text"], str):
                    parts.append(p["text"])
                elif p.get("type") in ("text", "output_text") and isinstance(p.get("text"), str):
                    parts.append(p["text"])
        return "\n".join(parts) if parts else json.dumps(content, ensure_ascii=False, indent=2)
    if isinstance(content, dict):
        if "text" in content and isinstance(content["text"], str):
            return content["text"]
        return json.dumps(content, ensure_ascii=False, indent=2)
    return "" if content is None else json.dumps(content, ensure_ascii=False, indent=2)

def line_to_md(obj):
    # Only process actual chat messages
    if obj.get("type") == "message":
        role = obj.get("role")
        content = obj.get("content")
        
        if role in ("user", "assistant") and content:
            text = extract_text(content)
            if text.strip():
                # Skip environment context messages
                if text.startswith("<environment_context>"):
                    return ""
                
                # Format the message
                role_display = "👤 User" if role == "user" else "🤖 Assistant"
                return f"## {role_display}\n\n{text}\n\n---\n\n"
    
    return ""

def convert(path):
    p = pathlib.Path(path)
    md = p.with_suffix(".md")
    with p.open("r", encoding="utf-8") as f, md.open("w", encoding="utf-8") as w:
        w.write(f"# Chat Conversation: {p.name}\n\n*Exported from Codex session*\n\n")
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except Exception:
                continue
            piece = line_to_md(obj)
            if piece:
                w.write(piece)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: jsonl-to-md.py path/to/rollout-*.jsonl")
        sys.exit(1)
    convert(sys.argv[1])
