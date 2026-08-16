"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { AlertTriangle, Building2, ChevronsUpDown, ClipboardList, FlaskConical, LayoutDashboard, LogOut, Map, Users } from "lucide-react";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  sidebarMenuButtonVariants,
} from "@/components/ui/sidebar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";

const navItems = [
  { title: "Dashboard", url: "/", icon: LayoutDashboard },
  { title: "Organizations", url: "/organizations", icon: Building2 },
  { title: "Members", url: "/members", icon: Users },
  { title: "Assessments", url: "/assessments", icon: ClipboardList },
  { title: "Intelligence Debt", url: "/intelligence-debt", icon: AlertTriangle },
  { title: "Roadmap", url: "/roadmap", icon: Map },
];

function initials(name: string | null | undefined, email: string | null | undefined) {
  const source = name || email || "?";
  return source
    .split(/[\s@.]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join("");
}

export function AppSidebar({
  user,
  signOutAction,
}: {
  user: { name?: string | null; email?: string | null };
  signOutAction: () => Promise<void>;
}) {
  const pathname = usePathname();

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <div className="flex items-center gap-2 px-2 py-1.5">
          <span className="flex size-6 shrink-0 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
            OS
          </span>
          <p className="truncate text-sm font-semibold text-sidebar-foreground group-data-[collapsible=icon]:hidden">
            Organizational Singularity
          </p>
        </div>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              {navItems.map((item) => {
                const isActive = pathname === item.url;
                return (
                  <SidebarMenuItem key={item.url}>
                    <SidebarMenuButton
                      isActive={isActive}
                      tooltip={item.title}
                      className={cn(
                        isActive &&
                          "bg-primary/10 font-medium text-primary hover:bg-primary/15 hover:text-primary data-active:bg-primary/10 data-active:text-primary"
                      )}
                      render={
                        <Link href={item.url}>
                          <item.icon />
                          <span>{item.title}</span>
                        </Link>
                      }
                    />
                  </SidebarMenuItem>
                );
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              {/* A plain <button>, not <SidebarMenuButton>: chaining two custom
                  components that each do their own render-prop merging (Base UI's
                  Menu.Trigger + SidebarMenuButton's own useRender) silently ate the
                  click handler in testing. A native element sidesteps that. */}
              <DropdownMenuTrigger
                render={
                  <button
                    type="button"
                    className={cn(sidebarMenuButtonVariants({ size: "lg" }), "w-full")}
                  >
                    <Avatar size="sm">
                      <AvatarFallback>{initials(user.name, user.email)}</AvatarFallback>
                    </Avatar>
                    <div className="grid flex-1 text-left text-sm leading-tight group-data-[collapsible=icon]:hidden">
                      <span className="truncate font-medium">{user.name ?? "Signed in"}</span>
                      <span className="truncate text-xs text-sidebar-foreground/70">
                        {user.email}
                      </span>
                    </div>
                    <ChevronsUpDown className="ml-auto group-data-[collapsible=icon]:hidden" />
                  </button>
                }
              />
              <DropdownMenuContent side="top" align="start" className="w-56">
                <DropdownMenuGroup>
                  <DropdownMenuLabel>{user.email}</DropdownMenuLabel>
                </DropdownMenuGroup>
                <DropdownMenuSeparator />
                <DropdownMenuItem render={<Link href="/me"><FlaskConical /> Diagnostics</Link>} />
                <DropdownMenuSeparator />
                <form action={signOutAction}>
                  <DropdownMenuItem
                    variant="destructive"
                    render={
                      <button type="submit" className="w-full">
                        <LogOut /> Sign out
                      </button>
                    }
                  />
                </form>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>

      <SidebarRail />
    </Sidebar>
  );
}
