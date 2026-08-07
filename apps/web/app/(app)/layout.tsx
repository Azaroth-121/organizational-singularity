import { signOut } from "@/auth";
import { verifySession } from "@/lib/dal";
import { AppSidebar } from "@/components/app-sidebar";
import { SidebarInset, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { Separator } from "@/components/ui/separator";
import { TooltipProvider } from "@/components/ui/tooltip";

export default async function AppShellLayout({ children }: { children: React.ReactNode }) {
  const session = await verifySession();

  async function signOutAction() {
    "use server";
    await signOut({ redirectTo: "/login" });
  }

  return (
    <TooltipProvider>
      <SidebarProvider>
        <AppSidebar user={session.user} signOutAction={signOutAction} />
        <SidebarInset>
          <header className="flex h-14 shrink-0 items-center gap-2 border-b border-sidebar-border px-4">
            <SidebarTrigger />
            <Separator orientation="vertical" className="h-4" />
          </header>
          <div className="flex flex-1 flex-col">{children}</div>
        </SidebarInset>
      </SidebarProvider>
    </TooltipProvider>
  );
}
