using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SmartDiningSystem.Application.DTOs.Bookings;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SmartDiningSystem.Api.Swagger;

public class CreateBookingRequestSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(CreateBookingRequestDto))
        {
            return;
        }

        if (schema.Properties.TryGetValue("reservationTime", out var reservationTimeSchema))
        {
            reservationTimeSchema.Example = new OpenApiString("2026-04-03 22:30");
            reservationTimeSchema.Description = "Baghdad local time in format yyyy-MM-dd HH:mm";
        }

        schema.Example = new OpenApiObject
        {
            ["tableId"] = new OpenApiString("42ba61d3-ed6e-4de7-9287-77cd923e6ba0"),
            ["reservationTime"] = new OpenApiString("2026-04-03 22:30"),
            ["items"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["menuItemId"] = new OpenApiString("41111111-1111-1111-1111-111111111113"),
                    ["quantity"] = new OpenApiInteger(2)
                }
            }
        };
    }
}
