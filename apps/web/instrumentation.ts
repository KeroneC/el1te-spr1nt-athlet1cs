import type { Instrumentation } from "next";

export const onRequestError: Instrumentation.onRequestError = async (error, _request, context) => {
  const digest = typeof error === "object" && error !== null && "digest" in error && typeof error.digest === "string"
    ? error.digest
    : undefined;
  console.error(JSON.stringify({
    eventName: "UnexpectedNextServerError",
    digest: digest?.slice(0, 64) ?? "not-available",
    routeTemplate: context.routePath,
    routeType: context.routeType,
    routerKind: context.routerKind,
    releaseSha: (process.env.RELEASE_SHA ?? "local").slice(0, 40)
  }));
};
