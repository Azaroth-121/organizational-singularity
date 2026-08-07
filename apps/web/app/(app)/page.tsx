import { getApiHealth } from "@/lib/api";
import { verifySession } from "@/lib/dal";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export default async function DashboardPage() {
  const session = await verifySession();
  const health = await getApiHealth();

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-sm text-muted-foreground">
          Welcome back, {session.user?.name ?? session.user?.email}.
        </p>
      </div>

      <Card className="max-w-md">
        <CardHeader>
          <CardTitle>System status</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-2 gap-y-3 text-sm">
            <dt className="text-muted-foreground">Web</dt>
            <dd>
              <Badge variant="outline" className="text-green-600 dark:text-green-400">
                Healthy
              </Badge>
            </dd>

            <dt className="text-muted-foreground">API</dt>
            <dd>
              <Badge
                variant={health ? "outline" : "destructive"}
                className={health ? "text-green-600 dark:text-green-400" : undefined}
              >
                {health ? "Healthy" : "Unreachable"}
              </Badge>
            </dd>

            <dt className="text-muted-foreground">Environment</dt>
            <dd className="font-medium">{health?.environment ?? "unknown"}</dd>

            <dt className="text-muted-foreground">API Version</dt>
            <dd className="font-medium">{health?.version ?? "unknown"}</dd>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}
