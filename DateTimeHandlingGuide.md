# DateTime Handling Guide for PostgreSQL - SOLUCIÓN GLOBAL

## Problem
PostgreSQL with `timestamp with time zone` columns requires UTC DateTime values. The application was using `DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)` which creates DateTime with `Kind=Unspecified`, causing the error:

```
ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported.
```

## SOLUCIÓN GLOBAL IMPLEMENTADA

### 1. **Interceptor Global de Entity Framework**
**Archivo:** `Models/DateTimeInterceptor.cs`

Este interceptor convierte automáticamente TODOS los DateTime a UTC antes de guardarlos en PostgreSQL:

```csharp
// Se ejecuta automáticamente en cada SaveChanges()
public class DateTimeInterceptor : ISaveChangesInterceptor
{
    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ConvertDateTimesToUtc(eventData.Context);
        return result;
    }
}
```

### 2. **Middleware Global para JSON**
**Archivo:** `Middleware/DateTimeMiddleware.cs`

Maneja automáticamente las conversiones de DateTime en las solicitudes HTTP:

```csharp
// Convertidores JSON personalizados
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Convierte automáticamente a UTC
        return dateTime.ToUniversalTime();
    }
}
```

### 3. **Filtro Global de Controladores**
**Archivo:** `Attributes/DateTimeConversionAttribute.cs`

Convierte automáticamente los DateTime en los parámetros de los controladores:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class DateTimeConversionAttribute : ActionFilterAttribute
{
    // Se ejecuta automáticamente en cada acción del controlador
}
```

### 4. **Servicio Global de DateTime**
**Archivo:** `Services/Implementations/GlobalDateTimeService.cs`

Proporciona métodos de extensión para manejar DateTime:

```csharp
// Uso en cualquier parte del código
DateTime.UtcNow.ToPostgreSqlDateTime()
dateTime.ToUtcIsoString()
```

### 5. **Configuración Global en Program.cs**

```csharp
// Configurar JSON global
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new DateTimeJsonConverter());
    options.SerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
});

// Configurar MVC global
builder.Services.AddControllers()
    .AddMvcOptions(options =>
    {
        options.Filters.Add<DateTimeConversionAttribute>();
    });

// Agregar middleware al pipeline
app.UseMiddleware<DateTimeMiddleware>();
```

## VENTAJAS DE LA SOLUCIÓN GLOBAL

### ✅ **Automático**
- No necesitas modificar cada archivo individualmente
- Se aplica automáticamente a todo el código existente y futuro

### ✅ **Transparente**
- El código existente sigue funcionando sin cambios
- Los desarrolladores pueden seguir usando `DateTime.UtcNow` normalmente

### ✅ **Consistente**
- Todas las conversiones de DateTime se manejan de la misma manera
- No hay inconsistencias entre diferentes partes del código

### ✅ **Mantenible**
- Un solo lugar para cambiar la lógica de DateTime
- Fácil de extender para casos especiales

## CÓMO FUNCIONA

### 1. **Al recibir datos JSON**
```javascript
// JavaScript envía
{
    "dateOfBirth": "2023-01-15T10:30:00"
}
```

**Middleware automáticamente convierte a:**
```csharp
// C# recibe
DateTime dateOfBirth = "2023-01-15T10:30:00Z" // UTC
```

### 2. **Al guardar en la base de datos**
```csharp
// Código normal
user.DateOfBirth = model.DateOfBirth;
await _context.SaveChangesAsync();
```

**Interceptor automáticamente convierte a:**
```sql
-- PostgreSQL recibe
INSERT INTO users (date_of_birth) VALUES ('2023-01-15T10:30:00Z')
```

### 3. **Al enviar datos JSON**
```csharp
// Código normal
return Json(new { dateOfBirth = user.DateOfBirth });
```

**Convertidor automáticamente convierte a:**
```javascript
// JavaScript recibe
{
    "dateOfBirth": "2023-01-15T10:30:00.000Z"
}
```

## USO EN EL CÓDIGO

### **Código existente (sin cambios necesarios)**
```csharp
// Esto sigue funcionando normalmente
user.CreatedAt = DateTime.UtcNow;
user.DateOfBirth = model.DateOfBirth;
await _context.SaveChangesAsync(); // Automáticamente convertido a UTC
```

### **Código nuevo (opcional)**
```csharp
// Puedes usar los métodos de extensión si quieres ser explícito
using SchoolManager.Services.Implementations;

user.CreatedAt = GlobalDateTimeService.GetCurrentUtcDateTime();
user.DateOfBirth = model.DateOfBirth.ToPostgreSqlDateTime();
```

## TESTING

### **Verificar que funciona**
1. **Login** - El error original debería estar resuelto
2. **Crear usuarios** - DateOfBirth se convierte automáticamente
3. **Crear actividades** - CreatedAt se convierte automáticamente
4. **APIs JSON** - DateTime se serializa/deserializa correctamente

### **Logs de conversión**
El sistema registra automáticamente las conversiones:
```
DateTime convertido a UTC: 2023-01-15 10:30:00 -> 2023-01-15 15:30:00Z
```

## MIGRACIÓN DE DATOS EXISTENTES

Si tienes datos existentes con `Kind=Unspecified`, ejecuta:

```sql
-- Convertir todos los DateTime existentes a UTC
UPDATE users SET 
    created_at = created_at AT TIME ZONE 'UTC',
    last_login = last_login AT TIME ZONE 'UTC',
    date_of_birth = date_of_birth AT TIME ZONE 'UTC'
WHERE created_at IS NOT NULL OR last_login IS NOT NULL OR date_of_birth IS NOT NULL;
```

## BENEFICIOS ADICIONALES

### **JavaScript/jQuery**
```javascript
// Enviar fechas
const date = new Date().toISOString(); // Automáticamente UTC

// Recibir fechas
const date = new Date(response.dateOfBirth); // Automáticamente UTC
```

### **Formularios HTML**
```html
<!-- Los DateTime se convierten automáticamente -->
<input type="datetime-local" name="dateOfBirth" />
```

### **APIs REST**
```csharp
// Automáticamente maneja DateTime en JSON
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateUserViewModel model)
{
    // model.DateOfBirth ya está en UTC automáticamente
    user.DateOfBirth = model.DateOfBirth;
    await _context.SaveChangesAsync(); // Automáticamente UTC
    return Ok(user);
}
```

## CONCLUSIÓN

Con esta solución global, **NO NECESITAS MODIFICAR NINGÚN CÓDIGO EXISTENTE**. El sistema maneja automáticamente todas las conversiones de DateTime para PostgreSQL, tanto para código existente como para código futuro.

¡La aplicación debería funcionar sin errores de DateTime! 🎉 