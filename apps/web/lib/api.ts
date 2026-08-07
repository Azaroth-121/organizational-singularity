const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

export interface ApiHealth {
  status: string;
  environment: string;
  version: string;
}

export async function getApiHealth(): Promise<ApiHealth | null> {
  try {
    const res = await fetch(`${API_BASE_URL}/api/v1/health`, { cache: "no-store" });
    if (!res.ok) return null;
    return (await res.json()) as ApiHealth;
  } catch {
    return null;
  }
}
