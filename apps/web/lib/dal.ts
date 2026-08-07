import "server-only";
import { cache } from "react";
import { redirect } from "next/navigation";
import { auth } from "@/auth";
import type { Session } from "next-auth";

type VerifiedSession = Session & { user: NonNullable<Session["user"]> };

// The actual auth gate for pages under app/(app). Per the Next.js auth guide bundled
// with this install, a layout-only check isn't reliable (partial rendering means a
// layout doesn't re-run on every navigation within its subtree, and doesn't stop child
// segments/Server Actions from still executing) -- so every page/action calls this
// directly instead of trusting app/(app)/layout.tsx alone. Wrapped in cache() so
// multiple calls within one request render only hit auth() once.
export const verifySession = cache(async (): Promise<VerifiedSession> => {
  const session = await auth();
  if (!session?.user) {
    redirect("/login");
  }
  return session as VerifiedSession;
});
