import type { NextConfig } from "next";

// When serving through a tunnel (ngrok/Cloudflare) for external testing, set AUTH_URL in
// .env.local to the public https URL. Without allowing that host here, the dev server
// rejects cross-origin requests outright and Server Actions fail their Origin/Host CSRF
// check, since both default to same-origin-only. Plain local dev leaves AUTH_URL unset,
// so this is a no-op.
const tunnelHost = process.env.AUTH_URL ? new URL(process.env.AUTH_URL).host : undefined;

const nextConfig: NextConfig = {
  output: "standalone",
  ...(tunnelHost && {
    allowedDevOrigins: [tunnelHost],
    experimental: {
      serverActions: {
        allowedOrigins: [tunnelHost],
      },
    },
  }),
};

export default nextConfig;
