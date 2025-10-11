# ??? Consumer Groups en Redis Streams - Guía Completa

## ?? **¿Qué es un Consumer Group?**

Un **Consumer Group** es como un **"equipo de trabajo"** en Redis que permite:
- ? **Distribución automática** de mensajes entre múltiples consumidores
- ? **Garantizar que cada mensaje se procese solo una vez** 
- ? **Reintento automático** si un consumidor falla
- ? **Escalabilidad horizontal** (agregar más consumidores)
- ? **Tolerancia a fallos** (si un consumidor muere, otro puede continuar)

---

## ??? **Estructura Visual:**

```
?? REDIS STREAM: "Notification:FinishLetterQueue"
?
??? ?? Entry: "1760134131299-0" ? {SriDocumentId: "ABC", Email: "test@test.com"}
??? ?? Entry: "1760134131300-1" ? {SriDocumentId: "DEF", Email: "user@user.com"}  
??? ?? Entry: "1760134131301-2" ? {SriDocumentId: "GHI", Email: "admin@admin.com"}
??? ?? Entry: "1760134131302-3" ? {SriDocumentId: "JKL", Email: "support@support.com"}

?? CONSUMER GROUP: "jiban-processors" 
??? ?? last-delivered-id: "1760134131301-2"  ? Solo entregar mensajes > este ID
??? ?? total-pending: 2                       ? Mensajes no confirmados
?
??? ?? Consumer: "api-instance-1" (activo hace 30s)
?   ??? ?? PEL (Pending Entries List):
?       ??? "1760134131299-0" (delivery-count: 1, idle: 2min)
?
??? ?? Consumer: "api-instance-2" (activo hace 10s) 
?   ??? ?? PEL (Pending Entries List):
?       ??? "1760134131300-1" (delivery-count: 3, idle: 5min) ? Reintentado 3 veces
?
??? ?? Consumer: "api-instance-3" (activo hace 5s)
    ??? ?? PEL (Pending Entries List): (vacía) ? Sin mensajes pendientes
```

---

## ?? **Flujo de procesamiento:**

### **1. Mensaje llega al Stream**
```bash
# Un productor envía un mensaje
XADD "Notification:FinishLetterQueue" * SriDocumentId "XYZ123" Email "new@example.com"
# Redis asigna ID: "1760134131303-0"
```

### **2. Consumer solicita mensajes nuevos**
```bash
# Tu ElectronicDocHostedService ejecuta (internamente):
XREADGROUP GROUP "jiban-processors" "api-instance-1" COUNT 1 STREAMS "Notification:FinishLetterQueue" >

# Redis responde:
# - Verifica: ¿hay mensajes con ID > "1760134131301-2"?
# - Encuentra: "1760134131303-0" 
# - Entrega mensaje a "api-instance-1"
# - Actualiza last-delivered-id = "1760134131303-0"
# - Agrega mensaje a PEL de "api-instance-1"
```

### **3. Procesamiento exitoso**
```bash
# Tu aplicación procesa exitosamente y confirma:
XACK "Notification:FinishLetterQueue" "jiban-processors" "1760134131303-0"

# Redis:
# - Elimina mensaje de PEL de "api-instance-1" 
# - Mensaje se considera "completado" ?
```

### **4. Procesamiento fallido**
```bash
# Si tu aplicación falla y NO hace XACK:
# - Mensaje permanece en PEL
# - Próximo ReadPendingMessagesAsync() lo recupera
# - Se incrementa delivery-count
# - Otro consumidor puede reclamarlo con XCLAIM
```

---

## ?? **Ventajas del Consumer Group:**

### **?? 1. Escalabilidad Automática**
```
Sin Consumer Group (problemático):
???????????????    ???????????????    ???????????????
?  Consumer A ?    ?  Consumer B ?    ?  Consumer C ?
? lee TODOS   ?    ? lee TODOS   ?    ? lee TODOS   ?
? los mensajes?    ? los mensajes?    ? los mensajes?
???????????????    ???????????????    ???????????????
     ?                    ?                    ?
     ??????? Duplicación de trabajo ???????????

Con Consumer Group (eficiente):
???????????????    ???????????????    ???????????????
?  Consumer A ?    ?  Consumer B ?    ?  Consumer C ?
? lee Msg 1,4 ?    ? lee Msg 2,5 ?    ? lee Msg 3,6 ?
?             ?    ?             ?    ?             ?
???????????????    ???????????????    ???????????????
     ?                    ?                    ?
     ??????? Distribución automática ??????????
```

### **?? 2. Garantía de Procesamiento Único**
- Cada mensaje se entrega a **UN SOLO consumidor**
- No hay duplicación accidental
- Redis coordina automáticamente

### **??? 3. Tolerancia a Fallos**
- Si un consumidor muere, sus mensajes pendientes quedan en PEL
- Otros consumidores pueden reclamarlos con XCLAIM
- Reintentos automáticos configurables

### **?? 4. Observabilidad**
```bash
# Ver estado del grupo
XINFO GROUPS "Notification:FinishLetterQueue"
XINFO CONSUMERS "Notification:FinishLetterQueue" "jiban-processors"  
XPENDING "Notification:FinishLetterQueue" "jiban-processors"
```

---

## ?? **En tu código:**

### **Creación del Consumer Group:**
```csharp
// Tu ElectronicDocHostedService hace:
await _eventService.CreateConsumerGroupAsync(
    "Notification:FinishLetterQueue",  // Stream name
    "jiban-processors"                 // Consumer Group name
);

// Redis equivalente:
// XGROUP CREATE "Notification:FinishLetterQueue" "jiban-processors" 0 MKSTREAM
```

### **Lectura de mensajes nuevos:**
```csharp
// ReadNewMessagesAsync() internamente ejecuta:
// XREADGROUP GROUP "jiban-processors" "unique-consumer-id" COUNT 10 STREAMS "Notification:FinishLetterQueue" >
//                     ?                    ?                                                                  ?
//                Group name          Consumer name                                                    ">" = nuevos mensajes
```

### **Confirmación de procesamiento:**
```csharp  
// DeleteMessageById() internamente ejecuta:
// XACK "Notification:FinishLetterQueue" "jiban-processors" "message-id"
//        ?                                ?                    ?
//   Stream name                    Group name            Message ID
```

---

## ? **¿Cómo Redis genera Consumer Names?**

Cada instancia de tu aplicación se convierte automáticamente en un **consumidor único**:

```csharp
// Cuando llamas ReadNewMessagesAsync(), Redis internamente:
string consumerName = $"{Environment.MachineName}-{ProcessId}-{ThreadId}";
// Ejemplo: "DESKTOP-ABC123-1234-5678"

// O puede usar un identificador más simple como:
// "consumer-1", "consumer-2", etc.
```

---

## ?? **Problemas comunes:**

### **1. Consumer Group no existe**
```bash
# Error: NOGROUP No such key 'stream-name' or consumer group 'group-name'
# Solución: Crear el grupo
XGROUP CREATE "Notification:FinishLetterQueue" "jiban-processors" 0 MKSTREAM
```

### **2. Mensajes acumulándose en PEL**
```bash
# Ver mensajes pendientes
XPENDING "Notification:FinishLetterQueue" "jiban-processors"

# Si hay muchos pendientes, verificar:
# - ¿Los consumidores están haciendo XACK?
# - ¿Hay excepciones impidiendo el XACK?
# - ¿Los consumidores están activos?
```

### **3. Consumidores inactivos**
```bash
# Ver consumidores activos
XINFO CONSUMERS "Notification:FinishLetterQueue" "jiban-processors"

# Limpiar consumidores inactivos
XGROUP DELCONSUMER "Notification:FinishLetterQueue" "jiban-processors" "old-consumer-name"
```

---

## ?? **En tu caso específico:**

Tu **Consumer Group "jiban-processors"** permite que:
- ? Múltiples instancias de tu API procesen mensajes en paralelo
- ? Cada mensaje de `EventoCuentaPymeModelo` se procese exactamente una vez
- ? Si una instancia falla, otra puede continuar con sus mensajes pendientes
- ? Puedas escalar horizontalmente agregando más instancias

**¡Es la base de un sistema de procesamiento distribuido robusto! ??**