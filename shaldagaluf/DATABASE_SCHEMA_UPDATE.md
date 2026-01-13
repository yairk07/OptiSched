# Database Schema Update Documentation

## Overview
This document describes the new tables added to the Access database and their relationships.

## New Tables Created

### 1. Files
Stores file metadata for uploaded files.
- **id** (AutoNumber, PK)
- **file_name** (Short Text, 255)
- **file_path** (Short Text, 500)
- **file_type** (Short Text, 100)
- **uploaded_at** (Date/Time)
- **uploaded_by** (Number) → Foreign Key to `Users.id`

### 2. EventFiles
Junction table linking events to files.
- **id** (AutoNumber, PK)
- **event_id** (Number) → Foreign Key to `CalendarEvents.Id`
- **file_id** (Number) → Foreign Key to `Files.id`

### 3. Images
Stores image metadata for uploaded images.
- **id** (AutoNumber, PK)
- **image_name** (Short Text, 255)
- **image_path** (Short Text, 500)
- **uploaded_at** (Date/Time)
- **uploaded_by** (Number) → Foreign Key to `Users.id`

### 4. EventImages
Junction table linking events to images.
- **id** (AutoNumber, PK)
- **event_id** (Number) → Foreign Key to `CalendarEvents.Id`
- **image_id** (Number) → Foreign Key to `Images.id`

### 5. ContactMessages
Stores contact form submissions.
- **id** (AutoNumber, PK)
- **full_name** (Short Text, 255)
- **email** (Short Text, 255)
- **subject** (Short Text, 255)
- **message** (Long Text/MEMO)
- **created_at** (Date/Time)

### 6. PermissionTypes
Lookup table for permission type definitions.
- **id** (AutoNumber, PK)
- **name** (Short Text, 255)
- **description** (Long Text/MEMO)

### 7. CalendarPermissions
Stores user permissions for shared calendars.
- **id** (AutoNumber, PK)
- **calendar_id** (Number) → Foreign Key to `SharedCalendars.Id`
- **user_id** (Number) → Foreign Key to `Users.id`
- **permission_type_id** (Number) → Foreign Key to `PermissionTypes.id`

### 8. CalendarJoinRequests
Stores requests to join shared calendars.
- **id** (AutoNumber, PK)
- **calendar_id** (Number) → Foreign Key to `SharedCalendars.Id`
- **user_id** (Number) → Foreign Key to `Users.id`
- **status** (Short Text, 50) - e.g., "Pending", "Approved", "Rejected"
- **requested_at** (Date/Time)

## Relationships

### Foreign Key Relationships:
1. **Files.uploaded_by** → **Users.id**
2. **EventFiles.event_id** → **CalendarEvents.Id**
3. **EventFiles.file_id** → **Files.id**
4. **Images.uploaded_by** → **Users.id**
5. **EventImages.event_id** → **CalendarEvents.Id**
6. **EventImages.image_id** → **Images.id**
7. **CalendarPermissions.calendar_id** → **SharedCalendars.Id**
8. **CalendarPermissions.user_id** → **Users.id**
9. **CalendarPermissions.permission_type_id** → **PermissionTypes.id**
10. **CalendarJoinRequests.calendar_id** → **SharedCalendars.Id**
11. **CalendarJoinRequests.user_id** → **Users.id**

## Important Notes

### Access Database Limitations
Microsoft Access has limited programmatic support for foreign key constraints. The tables are created with the appropriate data types, but foreign key relationships should be set up manually through the Access Relationships window for full referential integrity enforcement.

### To Set Up Relationships in Access:
1. Open the database file: `App_Data/calnder.db1.accdb.mdb`
2. Go to Database Tools → Relationships
3. Add the tables
4. Create relationships by dragging fields between tables
5. Enable "Enforce Referential Integrity" in the relationship dialog

### Running the Schema Update
To create the tables, navigate to:
- `http://localhost:PORT/createDatabaseTables.aspx`
- Click "Create Missing Tables" button

Or call programmatically:
```csharp
DatabaseSchemaUpdater.CreateMissingTables();
```

## Table Naming Convention
All tables use plural names (Files, Images, EventFiles, etc.) to maintain consistency with existing tables like `Users`, `CalendarEvents`, and `SharedCalendars`.

