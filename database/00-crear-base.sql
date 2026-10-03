-- Opcional para preparar la base desde SSMS.
-- El comando --initialize tambien puede crear esta base automaticamente.
USE [master];
GO
IF DB_ID(N'EscuelaControlDB') IS NULL
    CREATE DATABASE [EscuelaControlDB];
GO
USE [EscuelaControlDB];
GO
