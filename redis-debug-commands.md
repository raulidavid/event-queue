# ?? Comandos Redis CLI para Diagnosticar Cola de Mensajes

## ?? **1. VERIFICAR CONEXIÓN Y INFORMACIÓN GENERAL**

```bash
# Conectar a Redis
redis-cli -h desarrollo.jiban.ec

# Verificar conectividad
PING

# Ver información del servidor Redis
INFO server

# Listar todas las claves (cuidado en producción)
KEYS *Notification*
```

## ?? **2. INVESTIGAR EL STREAM PRINCIPAL**

```bash
# Ver información del stream
XINFO STREAM "Notification:FinishLetterQueue"

# Ver los mensajes en el stream (últimos 10)
XRANGE "Notification:FinishLetterQueue" - + COUNT 10

# Ver mensajes desde un ID específico (usa el ID de tu imagen)
XRANGE "Notification:FinishLetterQueue" "1760134131299-0" +

# Contar total de mensajes en el stream
XLEN "Notification:FinishLetterQueue"
```

## ?? **3. VERIFICAR CONSUMER GROUPS**

```bash
# Ver todos los consumer groups del stream
XINFO GROUPS "Notification:FinishLetterQueue"

# Ver información detallada del grupo "jiban-processors"
XINFO CONSUMERS "Notification:FinishLetterQueue" "jiban-processors"

# Crear el consumer group si no existe (desde el principio)
XGROUP CREATE "Notification:FinishLetterQueue" "jiban-processors" 0 MKSTREAM

# Crear el consumer group desde el final (solo mensajes nuevos)
XGROUP CREATE "Notification:FinishLetterQueue" "jiban-processors" $ MKSTREAM
```

## ?? **4. VERIFICAR MENSAJES PENDIENTES**

```bash
# Ver resumen de mensajes pendientes
XPENDING "Notification:FinishLetterQueue" "jiban-processors"

# Ver detalles de mensajes pendientes (primeros 10)
XPENDING "Notification:FinishLetterQueue" "jiban-processors" - + 10

# Ver mensajes pendientes de un consumidor específico
XPENDING "Notification:FinishLetterQueue" "jiban-processors" - + 10 consumer-1
```

## ?? **5. SIMULAR LECTURA DE MENSAJES**

```bash
# Leer mensajes nuevos (simular ReadNewMessagesAsync)
XREADGROUP GROUP "jiban-processors" "debug-consumer" COUNT 1 STREAMS "Notification:FinishLetterQueue" >

# Leer mensajes desde ID específico
XREADGROUP GROUP "jiban-processors" "debug-consumer" COUNT 10 STREAMS "Notification:FinishLetterQueue" 0

# Leer con timeout (bloqueo por 1000ms si no hay mensajes)
XREADGROUP GROUP "jiban-processors" "debug-consumer" BLOCK 1000 COUNT 1 STREAMS "Notification:FinishLetterQueue" >
```

## ? **6. GESTIÓN DE MENSAJES**

```bash
# Confirmar un mensaje específico (XACK)
XACK "Notification:FinishLetterQueue" "jiban-processors" "1760134131299-0"

# Reclamar mensajes pendientes (simular XCLAIM)
XCLAIM "Notification:FinishLetterQueue" "jiban-processors" "debug-consumer" 60000 "1760134131299-0"

# Eliminar mensajes del stream
XDEL "Notification:FinishLetterQueue" "1760134131299-0"
```

## ?? **7. LIMPIEZA Y RESET**

```bash
# Eliminar consumer group completo
XGROUP DESTROY "Notification:FinishLetterQueue" "jiban-processors"

# Eliminar todo el stream
DEL "Notification:FinishLetterQueue"

# Recrear consumer group desde el principio
XGROUP CREATE "Notification:FinishLetterQueue" "jiban-processors" 0 MKSTREAM
```

## ?? **8. COMANDOS DE DIAGNÓSTICO ESPECÍFICOS**

```bash
# Ver el contenido exacto de un mensaje específico
XRANGE "Notification:FinishLetterQueue" "1760134131299-0" "1760134131299-0"

# Ver qué consumidores están activos
XINFO CONSUMERS "Notification:FinishLetterQueue" "jiban-processors"

# Ver estadísticas del grupo de consumidores
XINFO GROUPS "Notification:FinishLetterQueue"
```

---

## ?? **SECUENCIA RECOMENDADA PARA DIAGNOSTICAR TU PROBLEMA:**

### **Paso 1: Verificar el stream**
```bash
redis-cli -h desarrollo.jiban.ec
XINFO STREAM "Notification:FinishLetterQueue"
XLEN "Notification:FinishLetterQueue"
```

### **Paso 2: Verificar consumer groups**
```bash
XINFO GROUPS "Notification:FinishLetterQueue"
```

### **Paso 3: Si no hay consumer groups, crear uno**
```bash
XGROUP CREATE "Notification:FinishLetterQueue" "jiban-processors" 0 MKSTREAM
```

### **Paso 4: Intentar leer mensajes**
```bash
XREADGROUP GROUP "jiban-processors" "test-consumer" COUNT 1 STREAMS "Notification:FinishLetterQueue" >
```

### **Paso 5: Verificar pendientes**
```bash
XPENDING "Notification:FinishLetterQueue" "jiban-processors"
```

---

## ?? **NOTAS IMPORTANTES:**

1. **Backup antes de modificar**: Los comandos `XGROUP DESTROY` y `DEL` son destructivos
2. **Consumer names**: Redis genera nombres únicos para consumidores automáticamente
3. **IDs de mensajes**: Formato timestamp-sequence (ej: `1760134131299-0`)
4. **Simbolo `>`**: Significa "mensajes más nuevos que el último entregado al grupo"
5. **Simbolo `$`**: Significa "desde el final del stream" (solo mensajes futuros)

---

## ?? **COMANDOS DE EMERGENCIA:**

Si necesitas resetear todo completamente:
```bash
# ?? CUIDADO: Esto elimina todos los datos
XGROUP DESTROY "Notification:FinishLetterQueue" "jiban-processors"
XGROUP CREATE "Notification:FinishLetterQueue" "jiban-processors" 0 MKSTREAM
```