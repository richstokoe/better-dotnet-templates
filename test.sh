#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TEST_DIR="$(mktemp -d /tmp/better-dotnet-templates-test-XXXXX)"
FAILURES=0
MVC_PID=""
HYBRID_PID=""
WEBAGENT_PID=""

cleanup() {
    [ -n "$MVC_PID" ]      && kill "$MVC_PID"      2>/dev/null && wait "$MVC_PID"      2>/dev/null || true
    [ -n "$HYBRID_PID" ]   && kill "$HYBRID_PID"   2>/dev/null && wait "$HYBRID_PID"   2>/dev/null || true
    [ -n "$WEBAGENT_PID" ] && kill "$WEBAGENT_PID" 2>/dev/null && wait "$WEBAGENT_PID" 2>/dev/null || true
    rm -rf "$TEST_DIR"
}
trap cleanup EXIT

pass() { echo "  PASS: $1"; }
fail() { echo "  FAIL: $1"; FAILURES=$((FAILURES + 1)); }

# Poll until URL responds with a non-5xx status or timeout expires.
wait_for_url() {
    local url="$1"
    local deadline=$(( $(date +%s) + 30 ))
    while [ "$(date +%s)" -lt "$deadline" ]; do
        local status
        status=$(curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null || true)
        [ "$status" != "000" ] && [ "$status" != "" ] && return 0
        sleep 1
    done
    return 1
}

# ── Step 1: Reinstall templates ──────────────────────────────────────────────
echo ""
echo "==> Reinstalling templates..."
cd "$SCRIPT_DIR"
bash reinstalltemplate.sh

# ── Step 2: better-agent ─────────────────────────────────────────────────────
echo ""
echo "==> Testing better-agent..."
cd "$TEST_DIR" && mkdir better-agent && cd better-agent
dotnet new better-agent -n TestBetterAgent -o TestBetterAgent
cd TestBetterAgent
if dotnet build; then
    pass "better-agent: dotnet build"
else
    fail "better-agent: dotnet build"
fi

# ── Step 3: better-mvc ───────────────────────────────────────────────────────
echo ""
echo "==> Testing better-mvc..."
cd "$TEST_DIR" && mkdir better-mvc && cd better-mvc
dotnet new better-mvc -n TestMvcApp -o TestMvcApp
cd TestMvcApp
if dotnet build; then
    pass "better-mvc: dotnet build"
else
    fail "better-mvc: dotnet build"
fi

MVC_PORT=5300
ASPNETCORE_URLS="http://localhost:$MVC_PORT" dotnet run --no-build --no-launch-profile &
MVC_PID=$!

echo "  Waiting for better-mvc to start on port $MVC_PORT..."
if wait_for_url "http://localhost:$MVC_PORT/"; then
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$MVC_PORT/")
    if [ "$HTTP_STATUS" = "200" ]; then
        pass "better-mvc: GET / returns 200"
    else
        fail "better-mvc: GET / returned $HTTP_STATUS (expected 200)"
    fi
else
    fail "better-mvc: app did not start within 30 seconds"
fi

kill "$MVC_PID" 2>/dev/null || true
wait "$MVC_PID" 2>/dev/null || true
MVC_PID=""

# ── Step 4: better-hybrid ────────────────────────────────────────────────────
echo ""
echo "==> Testing better-hybrid..."
cd "$TEST_DIR" && mkdir better-hybrid && cd better-hybrid
dotnet new better-hybrid -n TestHybridApp -o TestHybridApp
cd TestHybridApp
if dotnet build; then
    pass "better-hybrid: dotnet build (includes npm install + vite build)"
else
    fail "better-hybrid: dotnet build"
fi

HYBRID_PORT=5301
ASPNETCORE_URLS="http://localhost:$HYBRID_PORT" dotnet run --no-build --no-launch-profile &
HYBRID_PID=$!

echo "  Waiting for better-hybrid to start on port $HYBRID_PORT..."
if wait_for_url "http://localhost:$HYBRID_PORT/"; then
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$HYBRID_PORT/")
    if [ "$HTTP_STATUS" = "200" ]; then
        pass "better-hybrid: GET / returns 200 (SPA index.html)"
    else
        fail "better-hybrid: GET / returned $HTTP_STATUS (expected 200)"
    fi
else
    fail "better-hybrid: app did not start within 30 seconds"
fi

kill "$HYBRID_PID" 2>/dev/null || true
wait "$HYBRID_PID" 2>/dev/null || true
HYBRID_PID=""

# ── Step 5: better-webagent ──────────────────────────────────────────────────
echo ""
echo "==> Testing better-webagent..."
cd "$TEST_DIR" && mkdir better-webagent && cd better-webagent
dotnet new better-webagent -n TestWebAgentApp -o TestWebAgentApp
cd TestWebAgentApp
if dotnet build; then
    pass "better-webagent: dotnet build (includes npm install + vite build)"
else
    fail "better-webagent: dotnet build"
fi

WEBAGENT_PORT=5302
ASPNETCORE_URLS="http://localhost:$WEBAGENT_PORT" dotnet run --no-build --no-launch-profile &
WEBAGENT_PID=$!

echo "  Waiting for better-webagent to start on port $WEBAGENT_PORT..."
if wait_for_url "http://localhost:$WEBAGENT_PORT/"; then
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$WEBAGENT_PORT/")
    if [ "$HTTP_STATUS" = "200" ]; then
        pass "better-webagent: GET / returns 200 (SPA index.html)"
    else
        fail "better-webagent: GET / returned $HTTP_STATUS (expected 200)"
    fi
else
    fail "better-webagent: app did not start within 30 seconds"
fi

kill "$WEBAGENT_PID" 2>/dev/null || true
wait "$WEBAGENT_PID" 2>/dev/null || true
WEBAGENT_PID=""

# ── Summary ──────────────────────────────────────────────────────────────────
echo ""
if [ "$FAILURES" -eq 0 ]; then
    echo "All tests passed."
    exit 0
else
    echo "$FAILURES test(s) failed."
    exit 1
fi
