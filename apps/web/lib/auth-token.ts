import "server-only";
import { headers } from "next/headers";
import { getToken } from "next-auth/jwt";

// Reads the encrypted session cookie directly, bypassing the `session`
// callback in auth.ts so this never travels the same path as the
// browser-visible /api/auth/session response.
export async function getApiAccessToken(): Promise<string | null> {
  const token = await getToken({
    req: { headers: await headers() },
    secret: process.env.AUTH_SECRET!,
  });

  if (!token || token.apiAccessTokenError || !token.apiAccessToken) {
    return null;
  }

  return token.apiAccessToken;
}
