import { auth } from "@/auth";

// Public paths that must stay reachable while signed out. "/api/auth" is Auth.js's own
// callback/session/signout routes -- redirecting those would break sign-in itself.
const PUBLIC_PATHS = ["/login", "/api/auth"];

// Wrapping `auth` (rather than a bare re-export) still runs the jwt callback -- including
// the API token refresh check -- on every navigation, so the session cookie stays current.
// It additionally does a cheap, cookie-only redirect for signed-out users hitting anything
// else. This is only an "optimistic" check (per the Next.js auth guide): it unblocks a
// flash of protected UI, it is not the real authorization boundary -- that's
// lib/dal.ts's verifySession(), called from every page/action under app/(app).
export const proxy = auth((req) => {
  const isPublic = PUBLIC_PATHS.some((path) => req.nextUrl.pathname.startsWith(path));
  if (!req.auth && !isPublic) {
    return Response.redirect(new URL("/login", req.nextUrl.origin));
  }
});

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
