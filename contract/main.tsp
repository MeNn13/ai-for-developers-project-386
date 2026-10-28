import "@typespec/http";
import "@typespec/openapi";

using TypeSpec.Http;
using TypeSpec.OpenAPI;

@service(#{ title: "Booking Calendar API" })
@route("/api")
namespace BookingCalendar;

@route("/health")
@get
@operationId("getHealth")
op health(): HealthResponse;

model HealthResponse {
  status: "ok";
}
