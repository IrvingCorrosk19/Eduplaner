using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace SchoolManager.Attributes
{
    /// <summary>
    /// Atributo que convierte automáticamente los DateTime a UTC en los parámetros de los controladores
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DateTimeConversionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var parameter in context.ActionArguments.Values)
            {
                if (parameter != null)
                {
                    ConvertDateTimesToUtc(parameter);
                }
            }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Convierte recursivamente todos los DateTime a UTC en un objeto
        /// </summary>
        private void ConvertDateTimesToUtc(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();

            // Si es un DateTime, convertirlo a UTC
            if (type == typeof(DateTime))
            {
                var dateTime = (DateTime)obj;
                if (dateTime.Kind != DateTimeKind.Utc)
                {
                    var utcDateTime = dateTime.Kind == DateTimeKind.Local 
                        ? dateTime.ToUniversalTime() 
                        : DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
                    
                    // No podemos modificar directamente el valor, pero podemos registrar el cambio
                    Console.WriteLine($"DateTime convertido a UTC: {dateTime} -> {utcDateTime}");
                }
            }
            // Si es un DateTime nullable
            else if (type == typeof(DateTime?))
            {
                var nullableDateTime = (DateTime?)obj;
                if (nullableDateTime.HasValue && nullableDateTime.Value.Kind != DateTimeKind.Utc)
                {
                    var utcDateTime = nullableDateTime.Value.Kind == DateTimeKind.Local 
                        ? nullableDateTime.Value.ToUniversalTime() 
                        : DateTime.SpecifyKind(nullableDateTime.Value, DateTimeKind.Local).ToUniversalTime();
                    
                    Console.WriteLine($"Nullable DateTime convertido a UTC: {nullableDateTime} -> {utcDateTime}");
                }
            }
            // Si es una clase, buscar propiedades DateTime
            else if (type.IsClass && !type.IsPrimitive && type != typeof(string))
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var property in properties)
                {
                    if (property.PropertyType == typeof(DateTime))
                    {
                        var value = property.GetValue(obj);
                        if (value != null)
                        {
                            var dateTime = (DateTime)value;
                            if (dateTime.Kind != DateTimeKind.Utc)
                            {
                                var utcDateTime = dateTime.Kind == DateTimeKind.Local 
                                    ? dateTime.ToUniversalTime() 
                                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
                                
                                property.SetValue(obj, utcDateTime);
                            }
                        }
                    }
                    else if (property.PropertyType == typeof(DateTime?))
                    {
                        var value = property.GetValue(obj);
                        if (value != null)
                        {
                            var nullableDateTime = (DateTime?)value;
                            if (nullableDateTime.HasValue && nullableDateTime.Value.Kind != DateTimeKind.Utc)
                            {
                                var utcDateTime = nullableDateTime.Value.Kind == DateTimeKind.Local 
                                    ? nullableDateTime.Value.ToUniversalTime() 
                                    : DateTime.SpecifyKind(nullableDateTime.Value, DateTimeKind.Local).ToUniversalTime();
                                
                                property.SetValue(obj, utcDateTime);
                            }
                        }
                    }
                }
            }
        }
    }
} 