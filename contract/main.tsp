import "@typespec/http";
import "@typespec/openapi";

using TypeSpec.Http;
using TypeSpec.OpenAPI;

@service(#{ title: "Booking Calendar API" })
@server("http://127.0.0.1:5000", "Локальная разработка")
@doc("API сервиса бронирования календаря.")
@route("/api")
namespace BookingCalendar;

@route("/health")
@get
@tag("health")
@doc("Проверяет, что API отвечает.")
@operationId("getHealth")
op health(): HealthResponse;

@doc("Состояние API.")
@example(#{ status: "ok" })
model HealthResponse {
  @doc("Признак того, что API отвечает.")
  status: "ok";
}
