using BookingCalendar.Application.Abstractions;
using BookingCalendar.Application.Services;
using BookingCalendar.Application.State;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingCalendar.Application;

public static class ApplicationModule
{
    public static IServiceCollection AddBookingCalendarApplication(this IServiceCollection services)
    {
        services.AddSingleton<CalendarState>();
        services.AddScoped<IMeetingTypeService, MeetingTypeService>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
