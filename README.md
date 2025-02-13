# MeLi URL Shortener

## Estructura Actual del Proyecto
El proyecto está organizado en capas siguiendo Clean Architecture:

```
MeLi.UrlShortener/
├── src/
│   ├── MeLi.UrlShortener.Api/           # Capa de presentación
│   ├── MeLi.UrlShortener.Application/   # Lógica de aplicación
│   ├── MeLi.UrlShortener.Domain/        # Entidades y reglas de negocio
│   └── MeLi.UrlShortener.Infrastructure/# Implementaciones concretas
```

## Tecnologías Implementadas
- **.NET Core 8.0**
- **MongoDB** para persistencia
- **Redis** para caché

## Funcionalidades Implementadas
1. **API REST con endpoints para:**
   - Crear URLs cortas
   - Redireccionar URLs
   - Eliminar URLs
   - Obtener analytics

2. **Manejo de Cache**
   - Implementación con Redis
   - Circuit breaker pattern para resiliencia

3. **Persistencia**
   - MongoDB para almacenamiento
   - Índices optimizados

4. **Analytics**
   - Procesamiento batch con ConcurrentQueue
   - Actualización periódica de estadísticas

## Puntos de Mejora Identificados
1. **Performance y Escalabilidad**
   - Separar el servicio de analytics
   - Implementar message broker para desacoplar operaciones
   - Optimizar queries de MongoDB

2. **Monitoreo**
   - Mejorar la implementación de métricas
   - Configurar dashboards específicos

## Requerimientos del Challenge
- [x] Acortar URLs largas
- [x] Redireccionar URLs cortas
- [x] Obtener estadísticas
- [x] Manejo de 50,000 req/s (Pendiente pruebas de carga)
- [x] 90% de respuestas en <10ms (Pendiente validación)
- [x] Borrado de URLs
- [x] Redirección en navegador

## Decisiones Técnicas
1. **Clean Architecture**
   - Separación clara de responsabilidades
   - Inversión de dependencias
   - Value Objects para encapsulación

2. **Patrones Implementados**
   - Repository Pattern
   - Circuit Breaker
   - Background Processing

3. **Resilencia**
   - Manejo de errores centralizado
   - Políticas de retry
   - Circuit breakers para servicios externos

## TODO List
1. **Mejoras Críticas**
   - Separar analytics en servicio independiente
   - Implementar message broker
   - Optimizar queries y configuración de MongoDB

2. **Validaciones Pendientes**
   - Pruebas de carga
   - Validación de SLAs
   - Monitoreo en producción

## Configuración Actual
El proyecto utiliza:
- MongoDB para datos persistentes
- Redis para caché
- Background processing para analytics