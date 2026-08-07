import { getApiHealth } from "@/lib/api";

export default async function Home() {
  const health = await getApiHealth();

  return (
    <div className="flex flex-1 items-center justify-center bg-zinc-50 font-sans dark:bg-black">
      <main className="flex w-full max-w-xl flex-col gap-6 rounded-lg border border-zinc-200 bg-white p-10 dark:border-zinc-800 dark:bg-zinc-950">
        <div>
          <p className="text-sm font-medium tracking-wide text-zinc-500 uppercase">
            Organizational Singularity
          </p>
          <h1 className="text-2xl font-semibold text-zinc-950 dark:text-zinc-50">
            Development Environment
          </h1>
        </div>

        <dl className="grid grid-cols-2 gap-y-3 text-sm">
          <dt className="text-zinc-500">Web</dt>
          <dd className="font-medium text-green-600 dark:text-green-400">Healthy</dd>

          <dt className="text-zinc-500">API</dt>
          <dd
            className={`font-medium ${
              health ? "text-green-600 dark:text-green-400" : "text-red-600 dark:text-red-400"
            }`}
          >
            {health ? "Healthy" : "Unreachable"}
          </dd>

          <dt className="text-zinc-500">Environment</dt>
          <dd className="font-medium text-zinc-900 dark:text-zinc-100">
            {health?.environment ?? "unknown"}
          </dd>

          <dt className="text-zinc-500">API Version</dt>
          <dd className="font-medium text-zinc-900 dark:text-zinc-100">
            {health?.version ?? "unknown"}
          </dd>
        </dl>
      </main>
    </div>
  );
}
