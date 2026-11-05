import { serve } from "bun";
import index from "./index.html";

// Address of the C# API. Locally: dotnet run (port 5151). In docker-compose: the "server" service.
const API_URL = process.env.API_URL ?? "http://localhost:5151";

/**
 * Forwards /api/* to the C# backend. Because the browser only ever talks to this
 * server, the session cookie is same-origin (SameSite=Strict works, no CORS needed)
 * and the API does not have to be exposed to the outside.
 */
async function proxyToApi(req: Request, server: { requestIP(req: Request): { address: string } | null }) {
  const url = new URL(req.url);
  const target = new URL(url.pathname + url.search, API_URL);

  const headers = new Headers(req.headers);
  headers.delete("host");
  headers.delete("accept-encoding"); // ask for an unencoded body so it can be passed through unchanged
  // Overwrite (never append) so a client cannot spoof its address to dodge the login rate limit.
  headers.set("x-forwarded-for", server.requestIP(req)?.address ?? "unknown");

  const hasBody = req.method !== "GET" && req.method !== "HEAD";

  try {
    const upstream = await fetch(target, {
      method: req.method,
      headers,
      body: hasBody ? await req.arrayBuffer() : undefined,
      redirect: "manual",
    });
    return new Response(upstream.body, {
      status: upstream.status,
      statusText: upstream.statusText,
      headers: upstream.headers,
    });
  } catch {
    return Response.json({ title: "Bad gateway", detail: "The server is not reachable." }, { status: 502 });
  }
}

const server = serve({
  routes: {
    "/api/*": proxyToApi,

    // Serve index.html for all unmatched routes.
    "/*": index,
  },

  development: process.env.NODE_ENV !== "production" && {
    // Enable browser hot reloading in development
    hmr: true,

    // Echo console logs from the browser to the server
    console: true,
  },
});

console.log(`🚀 Client running at ${server.url} (API: ${API_URL})`);
