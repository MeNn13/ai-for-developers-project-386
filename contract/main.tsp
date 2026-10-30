import "@typespec/http";
import "@typespec/json-schema";
import "@typespec/openapi";

using TypeSpec.Http;
using TypeSpec.JsonSchema;
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

@route("/meeting-types")
@get
@tag("meeting-types")
@doc("Возвращает типы встреч в порядке создания; новый тип — в конце списка.")
@operationId("listMeetingTypes")
op listMeetingTypes(): MeetingType[];

@route("/meeting-types")
@post
@tag("meeting-types")
@doc("Создаёт тип встречи и возвращает созданный тип.")
@operationId("createMeetingType")
@opExample(
  #{
    returnType: #{
      statusCode: 400,
      body: #{
        type: "validation-failed",
        title: "Запрос некорректен",
        status: 400,
        detail: "Поля запроса не прошли проверку.",
        instance: "/api/meeting-types",
        errors: #{
          id: #["Разрешены только строчные латинские буквы, цифры и дефис"],
          durationMinutes: #["Длительность должна быть от 15 до 480 минут"],
        },
      },
    },
  },
  #{ description: "Id с недопустимыми символами и длительность вне диапазона." }
)
@opExample(
  #{
    returnType: #{
      statusCode: 409,
      body: #{
        type: "meeting-type-exists",
        title: "Конфликт",
        status: 409,
        detail: "Тип встречи с таким id уже существует.",
        instance: "/api/meeting-types",
      },
    },
  },
  #{ description: "Повторное создание типа с тем же id." }
)
op createMeetingType(@body body: MeetingTypeCreateRequest):
  | MeetingTypeCreatedResponse
  | ValidationError
  | MeetingTypeExistsError;

@doc("Тип встречи: шаблон с названием, описанием и длительностью.")
@example(#{
  id: "znakomstvo",
  title: "Знакомство",
  description: "Короткий первый созвон.",
  durationMinutes: 30,
})
model MeetingType {
  @doc("Короткий идентификатор типа встречи.")
  @minLength(1)
  @maxLength(64)
  @pattern("^[a-z0-9-]+$")
  id: string;

  @doc("Название типа встречи.")
  @minLength(1)
  @maxLength(120)
  title: string;

  @doc("Описание для гостя; пустая строка допустима.")
  @maxLength(500)
  description: string;

  @doc("Длительность встречи в минутах.")
  @minValue(15)
  @maxValue(480)
  @multipleOf(5)
  durationMinutes: int32;
}

@doc("Запрос на создание типа встречи.")
@example(#{
  id: "znakomstvo",
  title: "Знакомство",
  description: "Короткий первый созвон.",
  durationMinutes: 30,
})
model MeetingTypeCreateRequest {
  @doc("Короткий идентификатор типа встречи.")
  @minLength(1)
  @maxLength(64)
  @pattern("^[a-z0-9-]+$")
  id: string;

  @doc("Название типа встречи.")
  @minLength(1)
  @maxLength(120)
  title: string;

  @doc("Описание для гостя; пустая строка допустима.")
  @maxLength(500)
  description: string;

  @doc("Длительность встречи в минутах.")
  @minValue(15)
  @maxValue(480)
  @multipleOf(5)
  durationMinutes: int32;
}

alias ErrorSlug =
  | "validation-failed"
  | "slot-not-bookable"
  | "meeting-type-not-found"
  | "not-found"
  | "meeting-type-exists"
  | "slot-taken";

@doc("Описание ошибки в формате RFC 7807 Problem Details.")
@example(#{
  type: "not-found",
  title: "Не найдено",
  status: 404,
  detail: "Запрошенный маршрут API не существует.",
  instance: "/api/meeting-types",
})
model ProblemDetails {
  @doc("Короткий slug ошибки.")
  type: ErrorSlug;

  @doc("Краткий заголовок ошибки для человека.")
  title: string;

  @doc("HTTP-код ответа.")
  status: int32;

  @doc("Понятное описание ошибки для человека.")
  detail: string;

  @doc("Путь запроса, на котором произошла ошибка.")
  instance: string;

  @doc("Сообщения по полям; присутствует только у ошибки validation-failed.")
  errors?: Record<string[]>;
}

@doc("Тип встречи создан.")
model MeetingTypeCreatedResponse {
  @statusCode
  statusCode: 201;

  @body
  body: MeetingType;
}

@doc("Поля запроса не прошли проверку.")
model ValidationError {
  @statusCode
  statusCode: 400;

  @body
  body: ProblemDetails;
}

@doc("Тип встречи с таким id уже существует.")
model MeetingTypeExistsError {
  @statusCode
  statusCode: 409;

  @body
  body: ProblemDetails;
}
