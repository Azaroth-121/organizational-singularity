import { signIn } from "@/auth";

export default function LoginPage() {
  return (
    <div className="flex flex-1 items-center justify-center bg-zinc-50 font-sans dark:bg-black">
      <main className="flex w-full max-w-xl flex-col gap-6 rounded-lg border border-zinc-200 bg-white p-10 dark:border-zinc-800 dark:bg-zinc-950">
        <div>
          <p className="text-sm font-medium tracking-wide text-zinc-500 uppercase">
            Organizational Singularity
          </p>
          <h1 className="text-2xl font-semibold text-zinc-950 dark:text-zinc-50">
            Sign in
          </h1>
        </div>

        <form
          action={async () => {
            "use server";
            await signIn("microsoft-entra-id", { redirectTo: "/" });
          }}
        >
          <button
            type="submit"
            className="w-full rounded-md bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
          >
            Sign in with Microsoft
          </button>
        </form>
      </main>
    </div>
  );
}
