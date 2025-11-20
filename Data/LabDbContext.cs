using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using LabClinic.Api.Common;
using static LabClinic.Api.Data.DbContextSucursalHook;
using LabClinic.Api.Models;





namespace LabClinic.Api.Data
{
    // ============================================================
    //  DbContext
    // ============================================================
    public class LabDbContext : DbContext
    {
        private readonly ISucursalContext _sucCtx; 

        public LabDbContext(DbContextOptions<LabDbContext> opts, ISucursalContext sucCtx)
            : base(opts)
        {
            _sucCtx = sucCtx;
        }


        // ===== DbSets para parámetros y resultados =====
        public DbSet<Parametro> Parametros => Set<Parametro>();
        public DbSet<ParametroExamen> ParametrosExamen => Set<ParametroExamen>();     
        public DbSet<ExamenParametro> ExamenParametros => Set<ExamenParametro>();     

        // ===== DbSets (legacy/base) =====
        public DbSet<User> Users => Set<User>();
        public DbSet<Test> Tests => Set<Test>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<AppointmentTest> AppointmentTests => Set<AppointmentTest>();

        // ===== DbSets (V4 ampliado) =====
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Person> Persons => Set<Person>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<CategoryInsumo> CategoryInsumos => Set<CategoryInsumo>();
        public DbSet<Insumo> Insumos => Set<Insumo>();
        public DbSet<IngresoInsumo> IngresosInsumo => Set<IngresoInsumo>();
        public DbSet<TipoExamen> TiposExamen => Set<TipoExamen>();
        public DbSet<Examen> Examenes => Set<Examen>();
        public DbSet<TipoPago> TiposPago => Set<TipoPago>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<FacturaPago> Facturas => Set<FacturaPago>();
        public DbSet<Cita> Citas => Set<Cita>();
        public DbSet<FechaCita> FechasCita => Set<FechaCita>();
        public DbSet<RegistroHorarioFecha> RegistrosHorarioFecha => Set<RegistroHorarioFecha>();
        public DbSet<Empleado> Empleados => Set<Empleado>();
        public DbSet<Cargo> Cargos => Set<Cargo>();
        public DbSet<Bono> Bonos => Set<Bono>();
        public DbSet<Descuento> Descuentos => Set<Descuento>();
        public DbSet<Municipio> Municipios => Set<Municipio>();
        public DbSet<Departamento> Departamentos => Set<Departamento>();
        public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
        public DbSet<Planilla> Planillas => Set<Planilla>();
        public DbSet<Direccion> Direcciones => Set<Direccion>();
        public DbSet<VistaExamenReporteClinico> VistaExamenReporteClinico => Set<VistaExamenReporteClinico>();
        public DbSet<TipoExamenView> TipoExamenView => Set<TipoExamenView>();
        public DbSet<ParametroTipoExamen> ParametrosTipoExamen => Set<ParametroTipoExamen>();
        public DbSet<Clinica> Clinicas => Set<Clinica>();
        public DbSet<PrecioClinica> PreciosClinica => Set<PrecioClinica>();
        public DbSet<Person> Personas => Set<Person>();
        public DbSet<CategoriaTipoExamen> CategoriasTipoExamen => Set<CategoriaTipoExamen>();
        public DbSet<PerfilExamen> PerfilesExamen => Set<PerfilExamen>();
        public DbSet<PerfilParametro> PerfilParametros => Set<PerfilParametro>();
        public DbSet<Sucursal> Sucursales => Set<Sucursal>();
        public DbSet<HistorialIA> HistorialIA { get; set; }
        public DbSet<TipoExamenInsumo> TipoExamenInsumos => Set<TipoExamenInsumo>();
        public DbSet<RegistroInsumoUso> RegistroInsumoUsos { get; set; }  // ✅ agrega esto






        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // ===== USERS =====
            mb.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Firstname).HasColumnName("firstname");
                entity.Property(e => e.Middlename).HasColumnName("middlename");
                entity.Property(e => e.Lastname).HasColumnName("lastname");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.PasswordHash).HasColumnName("password");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.IdRol).HasColumnName("id_rol");

              
                entity.Property(e => e.IdPersona).HasColumnName("id_persona");

                //  Relaciones
                entity.HasOne(d => d.Rol)
                    .WithMany()
                    .HasForeignKey(d => d.IdRol)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Persona)
                    .WithMany()
                    .HasForeignKey(d => d.IdPersona)
                    .HasConstraintName("fk_user_persona")
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // ===== TEST =====s
            mb.Entity<Test>(e =>
            {
                e.ToTable("test_list");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.Code).HasColumnName("code");
                e.Property(x => x.Name).HasColumnName("name");
                e.Property(x => x.Description).HasColumnName("description");
                e.Property(x => x.Price).HasColumnName("price");
                e.Property(x => x.Status).HasColumnName("status");
            });

            // ===== CLIENT =====
            mb.Entity<Client>(e =>
            {
                e.ToTable("client_list");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.Code).HasColumnName("code");
                e.Property(x => x.Firstname).HasColumnName("firstname");
                e.Property(x => x.Middlename).HasColumnName("middlename");
                e.Property(x => x.Lastname).HasColumnName("lastname");
                e.Property(x => x.Gender).HasColumnName("gender");
                e.Property(x => x.Contact).HasColumnName("contact");
                e.Property(x => x.Address).HasColumnName("address");
                e.Property(x => x.Status).HasColumnName("status");
            });

            // ===== APPOINTMENT =====
            mb.Entity<Appointment>(e =>
            {
                e.ToTable("appointment_list");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.Code).HasColumnName("code");
                e.Property(x => x.Schedule).HasColumnName("schedule");
                e.Property(x => x.ClientId).HasColumnName("client_id");
                e.Property(x => x.PrescriptionPath).HasColumnName("prescription_path");
                e.Property(x => x.Status).HasColumnName("status");
            });

            mb.Entity<AppointmentTest>(e =>
            {
                e.ToTable("appointment_test_list");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.AppointmentId).HasColumnName("appointment_id");
                e.Property(x => x.TestId).HasColumnName("test_id");
                e.Property(x => x.Price).HasColumnName("price");
                e.Property(x => x.Status).HasColumnName("status");
            });

            // ===== ROLES =====
            mb.Entity<Role>(e =>
            {
                e.ToTable("rol");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_rol").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre_rol");
                e.Property(x => x.Descripcion).HasColumnName("descripcion");
            });

            // ===== PERSON =====
            mb.Entity<Person>(e =>
            {
                e.ToTable("persona");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_persona").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre");
                e.Property(x => x.Apellido).HasColumnName("apellido");
                e.Property(x => x.Sexo).HasColumnName("sexo");
                e.Property(x => x.Telefono).HasColumnName("telefono");
                e.Property(x => x.Correo).HasColumnName("correo");
                e.Property(x => x.Estado).HasColumnName("estado");
                e.Property(x => x.IdDireccion).HasColumnName("id_direccion");
                e.HasOne(x => x.Direccion)
                    .WithOne()
                    .HasForeignKey<Person>(x => x.IdDireccion)
                    .OnDelete(DeleteBehavior.Restrict);

                //  Relación Persona → Usuario
                e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
                e.HasOne(x => x.Usuario)
                    .WithMany()
                    .HasForeignKey(x => x.IdUsuario)
                    .OnDelete(DeleteBehavior.SetNull);

                e.Property(x => x.IdUsuarioCliente).HasColumnName("id_usuario_cliente");
                e.HasOne(x => x.UsuarioCliente)
                 .WithMany()
                 .HasForeignKey(x => x.IdUsuarioCliente)
                 .OnDelete(DeleteBehavior.SetNull);

            });


            // ===== DIRECCION =====
            mb.Entity<Direccion>(e =>
            {
                e.ToTable("direccion");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_direccion").ValueGeneratedOnAdd();
                e.Property(x => x.Calle).HasColumnName("calle");
                e.Property(x => x.Numero).HasColumnName("numero");
                e.Property(x => x.Zona).HasColumnName("zona");
                e.Property(x => x.Referencia).HasColumnName("referencia");
                e.Property(x => x.IdMunicipio).HasColumnName("idMunicipio");
                e.HasOne(x => x.Municipio)
                    .WithMany()
                    .HasForeignKey(x => x.IdMunicipio)
                    .HasConstraintName("FK_direccion_municipio")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== MUNICIPIO =====
            mb.Entity<Municipio>(e =>
            {
                e.ToTable("municipio");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("idMunicipio").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre");
                e.Property(x => x.IdDepartamento).HasColumnName("idDepartamento");
            });

            // ===== DEPARTAMENTO =====
            mb.Entity<Departamento>(e =>
            {
                e.ToTable("departamento");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("idDepartamento").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre");
            });

            // ===== SUPPLIER =====
            mb.Entity<Supplier>(e =>
            {
                e.ToTable("proveedor");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_proveedor").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre");
                e.Property(x => x.Empresa).HasColumnName("empresa");
                e.Property(x => x.Email).HasColumnName("email");
                e.Property(x => x.Telefono).HasColumnName("telefono");
                e.Property(x => x.Direccion).HasColumnName("direccion");
                e.Property(x => x.Descripcion).HasColumnName("descripcion");
                e.Property(x => x.Estado).HasColumnName("estado");
            });

            // ===== CATEGORY INSUMO =====
            mb.Entity<CategoryInsumo>(e =>
            {
                e.ToTable("categoriainsumo");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_categoria_insumo").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre_categoria");
                e.Property(x => x.Descripcion).HasColumnName("descripcion");
            });

            // ===== INSUMO =====
            mb.Entity<Insumo>(e =>
            {
                e.ToTable("insumolaboratorio");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id).HasColumnName("id_insumo").ValueGeneratedOnAdd();
                e.Property(x => x.IdCategoria).HasColumnName("id_categoria_insumo");
                e.Property(x => x.IdProveedor).HasColumnName("id_proveedor");
                e.Property(x => x.Nombre).HasColumnName("nombre_insumo");
                e.Property(x => x.Stock).HasColumnName("stock");
                e.Property(x => x.StockMinimo).HasColumnName("stock_minimo");
                e.Property(x => x.UnidadMedida).HasColumnName("unidad_medida");
                e.Property(x => x.Estado).HasColumnName("estado");
                e.Property(x => x.Almacenado).HasColumnName("almacenado");
                e.Property(x => x.Precio).HasColumnName("precio");
                e.Property(x => x.Descripcion).HasColumnName("descripcion");

                //  NUEVO: sucursal del insumo
                e.Property(x => x.IdSucursal).HasColumnName("id_sucursal");

                //  Relaciones (opcionales)
                e.HasOne(x => x.Sucursal)
                    .WithMany()
                    .HasForeignKey(x => x.IdSucursal)
                    .HasConstraintName("fk_insumo_sucursal")
                    .OnDelete(DeleteBehavior.SetNull);

                // ✅ Relación con Categoría
                e.HasOne(x => x.Categoria)
                    .WithMany()
                    .HasForeignKey(x => x.IdCategoria)
                    .HasConstraintName("fk_insumo_categoria")
                    .OnDelete(DeleteBehavior.SetNull);

            });

            mb.Entity<RegistroInsumoUso>(e =>
            {
                e.ToTable("registro_insumo_uso");
                e.HasKey(x => x.Id);
            });

            // ===== INGRESO INSUMO =====
            mb.Entity<IngresoInsumo>(e =>
            {
                e.ToTable("ingresoinsumo");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_ingreso").ValueGeneratedOnAdd();
                e.Property(x => x.IdInsumo).HasColumnName("id_insumo");
                e.Property(x => x.Cantidad).HasColumnName("cantidad");
                e.Property(x => x.FechaIngreso).HasColumnName("fecha_ingreso");
                e.Property(x => x.FechaExpira).HasColumnName("fecha_expira");
                e.Property(x => x.Descripcion).HasColumnName("descripcion");
            });


            // ===== CATEGORÍA TIPO EXAMEN =====
            mb.Entity<CategoriaTipoExamen>(e =>
            {
                e.ToTable("categoria_tipo_examen");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_categoria_tipo_examen").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre"); 
                e.Property(x => x.Descripcion).HasColumnName("descripcion");
            });


            // ===== TIPO EXAMEN =====
            mb.Entity<TipoExamen>(e =>
            {
                e.ToTable("tipo_examen");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_tipo_examen").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre");
                e.Property(x => x.Descripcion).HasColumnName("descripcion");
                e.Property(x => x.Precio).HasColumnName("precio");

                //  Relación con categoría
                e.Property(x => x.IdCategoriaTipoExamen).HasColumnName("id_categoria_tipo_examen");
                e.HasOne(x => x.Categoria)
                    .WithMany(c => c.TiposExamen)
                    .HasForeignKey(x => x.IdCategoriaTipoExamen)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_tipoexamen_categoria");

                // ===== PERFIL EN TIPO EXAMEN =====
                mb.Entity<TipoExamen>()
                    .Property(x => x.IdPerfilExamen)
                    .HasColumnName("id_perfil_examen");

                mb.Entity<TipoExamen>()
                    .HasOne(x => x.Perfil)
                    .WithMany()
                    .HasForeignKey(x => x.IdPerfilExamen)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_tipoexamen_perfil");

            });





            // ===== EXAMEN =====
            mb.Entity<Examen>(e =>
            {
                e.ToTable("examen");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_examen").ValueGeneratedOnAdd();
                e.Property(x => x.IdPersona).HasColumnName("id_persona");
                e.Property(x => x.IdTipoExamen).HasColumnName("id_tipo_examen");
                e.Property(x => x.IdInsumo).HasColumnName("id_insumo");
                e.Property(x => x.IdPago).HasColumnName("id_pago");
                e.Property(x => x.Resultado).HasColumnName("resultado");
                e.Property(x => x.Estado).HasColumnName("estado");
                e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").HasColumnType("datetime");
                e.Property(x => x.GrupoExamen).HasColumnName("grupo_examen").HasMaxLength(50);

                // Nueva relación con usuarios (médicos)
                e.Property(x => x.IdReferidor).HasColumnName("id_referidor");
                e.HasOne<User>(x => x.Referidor) 
                    .WithMany()
                    .HasForeignKey(x => x.IdReferidor)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_examen_referidor");
            });



            // ===== PARAMETRO (catálogo por tipo de examen) =====
            mb.Entity<Parametro>(e =>
            {
                e.ToTable("parametro");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_parametro").ValueGeneratedOnAdd();
                e.Property(x => x.IdTipoExamen).HasColumnName("id_tipo_examen");
                e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100);
                e.Property(x => x.Unidad).HasColumnName("unidad").HasMaxLength(50);
                e.Property(x => x.ValorReferencia).HasColumnName("valor_referencia").HasMaxLength(150);

                e.HasOne<TipoExamen>()
                    .WithMany(t => t.Parametros)
                    .HasForeignKey(p => p.IdTipoExamen)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // (refuerzo) relación desde TipoExamen
            mb.Entity<TipoExamen>()
              .HasMany(t => t.Parametros)
              .WithOne()
              .HasForeignKey(p => p.IdTipoExamen);

         
            // ===== EXAMEN_PARAMETRO (resultados por examen) =====
            mb.Entity<ExamenParametro>(e =>
            {
                e.ToTable("examen_parametro");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_examen_parametro").ValueGeneratedOnAdd();
                e.Property(x => x.IdExamen).HasColumnName("id_examen");
                e.Property(x => x.IdParametro).HasColumnName("id_parametro");
                e.Property(x => x.Valor).HasColumnName("valor");

                // Cambiar esta relación:
                e.HasOne<Examen>()
                 .WithMany(x => x.ExamenParametros)
                 .HasForeignKey(x => x.IdExamen)
                 .OnDelete(DeleteBehavior.Cascade);


                e.HasOne<Parametro>()
                 .WithMany()
                 .HasForeignKey(x => x.IdParametro)
                 .OnDelete(DeleteBehavior.Restrict);
            });


            // ===== PARAMETRO_EXAMEN (plantilla parametrizable por examen) =====
            mb.Entity<ParametroExamen>(e =>
            {
                e.ToTable("parametro_examen");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_parametro_examen").ValueGeneratedOnAdd();
                e.Property(x => x.IdExamen).HasColumnName("id_examen");
                e.Property(x => x.Nombre).HasColumnName("nombre");
                e.Property(x => x.Unidad).HasColumnName("unidad");
                e.Property(x => x.RangoReferencia).HasColumnName("rango_referencia");
                e.Property(x => x.Resultado).HasColumnName("resultado");
                e.Property(x => x.Observaciones).HasColumnName("observaciones");

                e.HasOne(x => x.Examen)
                    .WithMany()
                    .HasForeignKey(x => x.IdExamen)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== PARAMETRO TIPO EXAMEN =====
            mb.Entity<ParametroTipoExamen>(e =>
            {
                e.ToTable("parametro_tipo_examen");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id)
                    .HasColumnName("id_parametro_tipo_examen")
                    .ValueGeneratedOnAdd();

                e.Property(x => x.IdTipoExamen).HasColumnName("id_tipo_examen");
                e.Property(x => x.Nombre).HasColumnName("nombre");
                e.Property(x => x.Unidad).HasColumnName("unidad");
                e.Property(x => x.RangoReferencia).HasColumnName("rango_referencia");
                e.Property(x => x.Observaciones).HasColumnName("observaciones");

                //  Relación correcta sin inicializar listas
                e.HasOne(x => x.TipoExamen)
                 .WithMany(t => t.ParametrosTipo)
                 .HasForeignKey(x => x.IdTipoExamen)
                 .OnDelete(DeleteBehavior.Cascade)
                 .HasConstraintName("parametro_tipo_examen_ibfk_1");
            });



            // ===== TIPO PAGO =====
            mb.Entity<TipoPago>(e =>
            {
                e.ToTable("tipopago");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_tipo_pago").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre_tipo_pago");
            });

            // ===== PAGO =====
            mb.Entity<Pago>(e =>
            {
                e.ToTable("pago");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id).HasColumnName("id_pago");
                e.Property(x => x.IdPersona).HasColumnName("id_persona");
                e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
                e.Property(x => x.MontoPagado).HasColumnName("monto_pagado");
                e.Property(x => x.Concepto).HasColumnName("concepto");
                e.Property(x => x.IdTipoPago).HasColumnName("id_tipo_pago");
                e.Property(x => x.FechaGenerado).HasColumnName("fecha_generado");
                e.Property(x => x.FechaPago).HasColumnName("fecha_pago");
                e.Property(x => x.Nota).HasColumnName("nota");
                e.Property(x => x.Estado).HasColumnName("estado");

                //  NUEVA LÍNEA (multisucursal)
                e.Property(x => x.IdSucursal).HasColumnName("id_sucursal");

                e.HasOne(x => x.Persona)
                    .WithMany()
                    .HasForeignKey(x => x.IdPersona)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // ===== FACTURA =====
            mb.Entity<FacturaPago>(e =>
            {
                e.ToTable("facturapago");

                e.HasKey(x => x.Id);

                e.Property(x => x.Id)
                    .HasColumnName("id_factura")
                    .ValueGeneratedOnAdd();

                e.Property(x => x.IdPago)
                    .HasColumnName("id_pago");

                e.Property(x => x.FechaFactura)
                    .HasColumnName("fecha_factura");

                e.Property(x => x.MontoTotal)
                    .HasColumnName("monto_total");

                e.Property(x => x.Nit)
                    .HasColumnName("NIT");

                e.Property(x => x.Detalle)
                    .HasColumnName("Detalle");

                //  Relación explícita con Pago
                e.HasOne(f => f.Pago)
                    .WithMany(p => p.Facturas)
                    .HasForeignKey(f => f.IdPago)
                    .HasConstraintName("fk_factura_pago") 
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // ===== CITA =====
            mb.Entity<Cita>(e =>
            {
                e.ToTable("cita");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id).HasColumnName("id_cita").ValueGeneratedOnAdd();
                e.Property(x => x.IdPaciente).HasColumnName("id_paciente");
                e.Property(x => x.IdMedico).HasColumnName("id_medico");
                e.Property(x => x.Fecha).HasColumnName("fecha");
                e.Property(x => x.EstadoCita).HasColumnName("estado_cita");

                // Relaciones con claves foráneas
                e.HasOne(c => c.Paciente)
                    .WithMany()  // Si no tienes colección en Persona
                    .HasForeignKey(c => c.IdPaciente)
                    .HasConstraintName("fk_cita_persona")
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(c => c.Medico)
                    .WithMany()  // Si no tienes colección en User
                    .HasForeignKey(c => c.IdMedico)
                    .HasConstraintName("fk_cita_medico")
                    .OnDelete(DeleteBehavior.SetNull);
            });



            // ===== REGISTRO HORARIO FECHA =====
            mb.Entity<RegistroHorarioFecha>(e =>
            {
                e.ToTable("registrohorariofechas");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_registro").ValueGeneratedOnAdd();
                e.Property(x => x.IdFecha).HasColumnName("id_fecha");
                e.Property(x => x.HoraInicio).HasColumnName("hora_inicio");
                e.Property(x => x.HoraFin).HasColumnName("hora_fin");
            });

            // ===== EMPLEADO =====
            mb.Entity<Empleado>(e =>
            {
                e.ToTable("empleado");

                e.HasKey(x => x.Id);
                e.Property(x => x.Id)
                    .HasColumnName("id_empleado")
                    .ValueGeneratedOnAdd();

                e.Property(x => x.Nombre)
                    .HasColumnName("nombre")
                    .HasMaxLength(100)
                    .IsRequired();

                e.Property(x => x.Apellido)
                    .HasColumnName("apellido")
                    .HasMaxLength(100)
                    .IsRequired();

                e.Property(x => x.Sexo)
                    .HasColumnName("sexo")
                    .HasMaxLength(15)
                    .IsRequired();

                e.Property(x => x.Telefono)
                    .HasColumnName("telefono")
                    .HasMaxLength(30);

                e.Property(x => x.Correo)
                    .HasColumnName("correo")
                    .HasMaxLength(100);

                e.Property(x => x.FechaNacimiento)
                    .HasColumnName("fecha_nacimiento")
                    .HasColumnType("date");

                e.Property(x => x.IdMunicipio)
                    .HasColumnName("idMunicipio");

                e.Property(x => x.IdDepartamento)
                    .HasColumnName("id_departamento");

                e.Property(x => x.Dpi)
                    .HasColumnName("dpi")
                    .HasMaxLength(25)
                    .IsRequired();

                e.Property(x => x.FormacionAcademica)
                    .HasColumnName("formacion_academica")
                    .HasMaxLength(150)
                    .IsRequired();

                e.Property(x => x.Estado)
                    .HasColumnName("estado")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(1)
                    .IsRequired();
            });

            // ===== CARGO =====
            mb.Entity<Cargo>(e =>
            {
                e.ToTable("cargo");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_cargo").ValueGeneratedOnAdd();
                e.Property(x => x.NombreCargo).HasColumnName("nombre_cargo");
                e.Property(x => x.Funcion).HasColumnName("funcion");
                e.Property(x => x.Requisito).HasColumnName("requisito");
            });

            // ===== BONO =====
            mb.Entity<Bono>(e =>
            {
                e.ToTable("bono");
                e.HasKey(x => x.IdBono);

                e.Property(x => x.IdBono).HasColumnName("id_bono").ValueGeneratedOnAdd();
                e.Property(x => x.IdMedico).HasColumnName("id_medico");
                e.Property(x => x.IdPersona).HasColumnName("id_persona");
                e.Property(x => x.NombreBono).HasColumnName("nombre_bono").HasMaxLength(120);
                e.Property(x => x.MontoBono).HasColumnName("monto_bono").HasColumnType("decimal(12,2)");
                e.Property(x => x.Porcentaje).HasColumnName("porcentaje").HasColumnType("decimal(5,2)");
                e.Property(x => x.Estado).HasColumnName("estado");
                e.Property(x => x.Pagado).HasColumnName("pagado").HasDefaultValue(0);
                e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").HasColumnType("datetime");

                //  Relación con User (médico)
                e.HasOne(b => b.Medico)
                    .WithMany()
                    .HasForeignKey(b => b.IdMedico)
                    .HasPrincipalKey(u => u.Id) 
                    .OnDelete(DeleteBehavior.Restrict);

                //  Relación con Person (paciente)
                e.HasOne(b => b.Persona)
                    .WithMany(p => p.BonosReferidos)
                    .HasForeignKey(b => b.IdPersona)
                    .HasPrincipalKey(p => p.Id) 
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ===== DESCUENTO =====
            mb.Entity<Descuento>(e =>
            {
                e.ToTable("descuento");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_descuento").ValueGeneratedOnAdd();
                e.Property(x => x.IdEmpleado).HasColumnName("id_empleado");
                e.Property(x => x.Monto).HasColumnName("monto");
                e.Property(x => x.Descripcion).HasColumnName("descripcion");
            });

            // ===== COTIZACION =====
            mb.Entity<Cotizacion>(e =>
            {
                e.ToTable("cotizacion");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_cotizacion").ValueGeneratedOnAdd();
                e.Property(x => x.IdExamen).HasColumnName("id_examen");
                e.Property(x => x.Precio).HasColumnName("precio");
                e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
                e.Property(x => x.IdPago).HasColumnName("id_pago");

                e.HasOne(x => x.Examen)
                    .WithMany()
                    .HasForeignKey(x => x.IdExamen)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Pago)
                    .WithMany()
                    .HasForeignKey(x => x.IdPago)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // ===== PLANILLA =====
            mb.Entity<Planilla>(e =>
            {
                e.ToTable("planilla");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_planilla").ValueGeneratedOnAdd();
                e.Property(x => x.IdEmpleado).HasColumnName("id_empleado");
                e.Property(x => x.Puesto).HasColumnName("puesto");
                e.Property(x => x.IdCargo).HasColumnName("id_cargo");
                e.Property(x => x.SalarioBase).HasColumnName("salariobase");
                e.Property(x => x.FechaInicio).HasColumnName("fechainicio");
                e.Property(x => x.FechaFin).HasColumnName("fechafin");
                e.Property(x => x.NoCuenta).HasColumnName("nocuenta");
                e.Property(x => x.Banco).HasColumnName("banco");
            });
            // ===== VISTA EXAMEN REPORTE CLINICO =====
            mb.Entity<VistaExamenReporteClinico>(e =>
            {
                e.HasNoKey();
                e.ToView("vista_examen_reporte_clinico");
            });

            mb.Entity<TipoExamenView>(e =>
            {
                e.HasNoKey();
                e.ToView("tipoexamen"); 
            });

            // ===== CLINICA =====
            mb.Entity<Clinica>(e =>
            {
                e.ToTable("clinica");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_clinica").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre");
                e.Property(x => x.Direccion).HasColumnName("direccion");
                e.Property(x => x.Telefono).HasColumnName("telefono");
                e.Property(x => x.Contacto).HasColumnName("contacto");
                e.Property(x => x.Activo).HasColumnName("activo"); 
            });



            // ===== PRECIO CLINICA =====
            mb.Entity<PrecioClinica>(e =>
            {
                e.ToTable("precio_clinica");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_precio_clinica").ValueGeneratedOnAdd();
                e.Property(x => x.IdClinica).HasColumnName("id_clinica");
                e.Property(x => x.IdTipoExamen).HasColumnName("id_tipo_examen");
                e.Property(x => x.PrecioEspecial)
                    .HasColumnName("precio_especial")
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0);

                
                e.Property(x => x.VigenteDesde)
                    .HasColumnName("vigente_desde")
                    .HasColumnType("datetime")
                    .IsRequired(false);
                e.Property(x => x.VigenteHasta)
                    .HasColumnName("vigente_hasta")
                    .HasColumnType("datetime")
                    .IsRequired(false);

                //  Relaciones
                e.HasOne(x => x.Clinica)
                    .WithMany(c => c.PreciosClinica)
                    .HasForeignKey(x => x.IdClinica)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_precio_clinica_clinica");

                e.HasOne(x => x.TipoExamen)
                    .WithMany()
                    .HasForeignKey(x => x.IdTipoExamen)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_precio_clinica_tipo_examen");

            
                e.HasIndex(x => new { x.IdClinica, x.IdTipoExamen })
                    .IsUnique()
                    .HasDatabaseName("UQ_PrecioClinica");
            });

            // ===== PERFIL EXAMEN =====
            mb.Entity<PerfilExamen>(e =>
            {
                e.ToTable("perfil_examen");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_perfil_examen").ValueGeneratedOnAdd();
                e.Property(x => x.Nombre).HasColumnName("nombre");
                e.Property(x => x.Descripcion).HasColumnName("descripcion");

                // Relación con PerfilParametro
                e.HasMany(x => x.PerfilParametros)
                 .WithOne(p => p.PerfilExamen)
                 .HasForeignKey(p => p.IdPerfilExamen)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== PERFIL PARAMETRO =====
            mb.Entity<PerfilParametro>(e =>
            {
                e.ToTable("perfil_parametro");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_perfil_parametro").ValueGeneratedOnAdd();
                e.Property(x => x.IdPerfilExamen).HasColumnName("id_perfil_examen");
                e.Property(x => x.IdTipoExamen).HasColumnName("id_tipo_examen");

                e.HasOne(p => p.PerfilExamen)
                 .WithMany(pe => pe.PerfilParametros)
                 .HasForeignKey(p => p.IdPerfilExamen)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(p => p.TipoExamen)
                 .WithMany()
                 .HasForeignKey(p => p.IdTipoExamen)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            mb.Entity<TipoExamenInsumo>(e =>
            {
                e.ToTable("tipo_examen_insumo");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(x => x.IdTipoExamen).HasColumnName("id_tipo_examen");
                e.Property(x => x.IdInsumo).HasColumnName("id_insumo");
                e.Property(x => x.CantidadUsada)
                    .HasColumnName("cantidad_usada")
                    .HasColumnType("decimal(10,2)");

                // 🔹 IMPORTANTE: especificar nombres explícitos de FK y eliminar duplicación
                e.HasOne(x => x.TipoExamen)
                    .WithMany(t => t.InsumosAsociados)
                    .HasForeignKey(x => x.IdTipoExamen)
                    .HasConstraintName("fk_tipo_examen_insumo_tipo_examen")
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Insumo)
                    .WithMany()
                    .HasForeignKey(x => x.IdInsumo)
                    .HasConstraintName("fk_tipo_examen_insumo_insumo")
                    .OnDelete(DeleteBehavior.Restrict);
            });


            //  Filtros globales por sucursal
            mb.Entity<Examen>().HasQueryFilter(e => e.IdSucursal == _sucCtx.CurrentSucursalId);
            mb.Entity<Person>().HasQueryFilter(p => p.IdSucursal == _sucCtx.CurrentSucursalId);
            mb.Entity<Clinica>().HasQueryFilter(c => c.IdSucursal == _sucCtx.CurrentSucursalId);
            mb.Entity<PrecioClinica>().HasQueryFilter(pc => pc.IdSucursal == _sucCtx.CurrentSucursalId);
            mb.Entity<Bono>().HasQueryFilter(b => b.IdSucursal == _sucCtx.CurrentSucursalId);
            mb.Entity<Insumo>().HasQueryFilter(i => i.IdSucursal == _sucCtx.CurrentSucursalId);



        }

        // ============================================================
        //  Métodos de guardado con contexto de sucursal
        // ============================================================
        public override int SaveChanges()
        {
            this.StampSucursal(_sucCtx); 
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            this.StampSucursal(_sucCtx);
            return await base.SaveChangesAsync(cancellationToken);
        }

    }

    // ============================================================
    //  POCOs
    // ============================================================

    public class User
    {
        public int Id { get; set; }
        public string Firstname { get; set; } = "";
        public string? Middlename { get; set; }
        public string Lastname { get; set; } = "";
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public int Type { get; set; }
        public int Status { get; set; }
        public int IdRol { get; set; }
        public Role? Rol { get; set; }

        [Column("id_persona")]
        public int? IdPersona { get; set; }

        [ForeignKey(nameof(IdPersona))]
        [JsonIgnore]
        public Person? Persona { get; set; }
    }


    public class Test { public int Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string? Description { get; set; } public decimal Price { get; set; } public int Status { get; set; } }
    public class Client { public int Id { get; set; } public string Code { get; set; } = ""; public string Firstname { get; set; } = ""; public string? Middlename { get; set; } public string Lastname { get; set; } = ""; public string? Gender { get; set; } public string? Contact { get; set; } public string? Address { get; set; } public int Status { get; set; } }
    public class Appointment { public int Id { get; set; } public string Code { get; set; } = ""; public DateTime Schedule { get; set; } public int ClientId { get; set; } public int Status { get; set; } public string? PrescriptionPath { get; set; } }
    public class AppointmentTest { public int Id { get; set; } public int AppointmentId { get; set; } public int TestId { get; set; } public decimal Price { get; set; } public int Status { get; set; } }

    public class Role { public int Id { get; set; } public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } }

    [Table("persona")]
    public class Person
    {
        [Key]
        [Column("id_persona")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = null!;

        [Column("apellido")]
        public string Apellido { get; set; } = null!;

        [Column("sexo")]
        public string? Sexo { get; set; }

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("correo")]
        public string? Correo { get; set; }

        [Column("dpi")]
        public string? Dpi { get; set; }

        [Column("fecha_nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Column("estado")]
        public int Estado { get; set; } = 1;

        [Column("tipo_cliente")]
        public int TipoCliente { get; set; } = 0;

        [Column("id_direccion")]
        public int? IdDireccion { get; set; }

        [ForeignKey("IdDireccion")]
        public Direccion? Direccion { get; set; }

        [JsonIgnore]
        public ICollection<Bono>? BonosReferidos { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("id_clinica")]
        public int? IdClinica { get; set; }

        [ForeignKey(nameof(IdClinica))]
        public Clinica? Clinica { get; set; }

        //Sucursal a la que pertenece la persona/paciente
        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }


        //  Referido (ya existente)
        [Column("referido_por_id")]
        public int? ReferidoPorId { get; set; }

        [Column("referido_por")]
        public string? ReferidoPor { get; set; }

        [ForeignKey(nameof(ReferidoPorId))]
        [JsonIgnore]
        public User? MedicoReferidor { get; set; }

       
        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public User? Usuario { get; set; }   //  Esto enlaza con la tabla `users`

        [Column("id_usuario_cliente")]
        public int? IdUsuarioCliente { get; set; }

        [ForeignKey(nameof(IdUsuarioCliente))]
        public User? UsuarioCliente { get; set; }

    }

    public class Direccion
    {
        public int Id { get; set; }
        public string? Calle { get; set; }
        public string? Numero { get; set; }
        public string? Zona { get; set; }
        public string? Referencia { get; set; }
        public int? IdMunicipio { get; set; }
        [ForeignKey(nameof(IdMunicipio))] public Municipio? Municipio { get; set; }
    }

    public class Municipio { public int Id { get; set; } public string Nombre { get; set; } = ""; public int IdDepartamento { get; set; } [ForeignKey(nameof(IdDepartamento))] public Departamento? Departamento { get; set; } }
    public class Departamento { public int Id { get; set; } public string Nombre { get; set; } = ""; }

    public class Supplier { public int Id { get; set; } public string Nombre { get; set; } = ""; public string? Empresa { get; set; } public string? Email { get; set; } public string? Telefono { get; set; } public string? Direccion { get; set; } public string? Descripcion { get; set; } public int Estado { get; set; } }
    [Table("categoriainsumo")]
    public class CategoryInsumo
    {
        [Key]
        [Column("id_categoria_insumo")]
        public int Id { get; set; }

        [Column("nombre_categoria")]
        public string Nombre { get; set; } = "";

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("unidad")]
        public string? Unidad { get; set; } // ✅ Nueva columna heredable
    }

    [Table("insumolaboratorio")]
    public class Insumo
    {
        [Key]
        [Column("id_insumo")]
        public int Id { get; set; }

        [Column("id_categoria_insumo")]
        public int IdCategoria { get; set; }

        [Column("id_proveedor")]
        public int? IdProveedor { get; set; }

        [Column("nombre_insumo")]
        public string Nombre { get; set; } = "";

        [Column("stock")]
        public decimal Stock { get; set; }

        [Column("stock_minimo")]
        public decimal StockMinimo { get; set; }

        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }

        [Column("estado")]
        public int Estado { get; set; }

        [Column("almacenado")]
        public string? Almacenado { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

      
        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }

        [ForeignKey(nameof(IdCategoria))]
        public CategoryInsumo? Categoria { get; set; }
    }


    public class IngresoInsumo { public int Id { get; set; } public int IdInsumo { get; set; } public decimal Cantidad { get; set; } public DateTime FechaIngreso { get; set; } public DateTime? FechaExpira { get; set; } public string? Descripcion { get; set; } }

    public class TipoExamen
    {
        [Key]
        [Column("id_tipo_examen")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("precio")]
        public decimal? Precio { get; set; }

        [Column("id_categoria_tipo_examen")]
        public int? IdCategoriaTipoExamen { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdCategoriaTipoExamen))]
        public CategoriaTipoExamen? Categoria { get; set; }

        [Column("id_perfil_examen")]
        public int? IdPerfilExamen { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdPerfilExamen))]
        public PerfilExamen? Perfil { get; set; }

        [JsonIgnore]
        public ICollection<Parametro>? Parametros { get; set; }

        [JsonIgnore]
        public ICollection<ParametroTipoExamen>? ParametrosTipo { get; set; }

        [JsonIgnore]
        public ICollection<TipoExamenInsumo>? InsumosAsociados { get; set; }

    }

    [Table("categoria_tipo_examen")]
    public class CategoriaTipoExamen
    {
        [Key]
        [Column("id_categoria_tipo_examen")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [JsonIgnore]
        public ICollection<TipoExamen>? TiposExamen { get; set; }
    }





    public class Examen
    {
        [Key]
        [Column("id_examen")]
        public int Id { get; set; }

        [Column("id_persona")]
        public int IdPersona { get; set; }

        [Column("id_tipo_examen")]
        public int IdTipoExamen { get; set; }

        [Column("id_insumo")]
        public int? IdInsumo { get; set; }

        [Column("grupo_examen")]
        [MaxLength(50)]
        public string? GrupoExamen { get; set; }

       
        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }  

      
        [Column("id_pago")]
        public int? IdPago { get; set; }

        [ForeignKey(nameof(IdPago))]
        public Pago? Pago { get; set; }

        [Column("resultado")]
        public string? Resultado { get; set; }

        [Column("estado")]
        public int Estado { get; set; }

        [JsonIgnore]
        public ICollection<ExamenParametro>? ExamenParametros { get; set; }

        [ForeignKey(nameof(IdTipoExamen))]
        public TipoExamen? TipoExamen { get; set; }

        [ForeignKey(nameof(IdPersona))]
        public Person? Persona { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("id_clinica")]
        public int? IdClinica { get; set; }

        [Column("usar_precio_clinica")]
        public bool UsarPrecioClinica { get; set; } = false;

        [Column("precio_aplicado", TypeName = "decimal(10,2)")]
        public decimal PrecioAplicado { get; set; } = 0;

        [ForeignKey(nameof(IdClinica))]
        public Clinica? Clinica { get; set; }

        [Column("id_referidor")]
        public int? IdReferidor { get; set; }

        [ForeignKey(nameof(IdReferidor))]
        public User? Referidor { get; set; }
    }

    [Table("sucursal")]
    public class Sucursal
    {
        [Key]
        [Column("id_sucursal")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("direccion")]
        public string? Direccion { get; set; }
    }



    public class TipoPago { public int Id { get; set; } public string Nombre { get; set; } = ""; }

    [Table("pago")]
    public class Pago : ISucursalScoped
    {
        [Key]
        [Column("id_pago")]
        public int Id { get; set; }

        [Column("id_persona")]
        public int? IdPersona { get; set; }

        [ForeignKey(nameof(IdPersona))]
        public Person? Persona { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("monto_pagado", TypeName = "decimal(10,2)")]
        public decimal MontoPagado { get; set; }

        [Column("concepto")]
        public string? Concepto { get; set; }

        [Column("id_tipo_pago")]
        public int IdTipoPago { get; set; }

        [Column("fecha_generado")]
        public DateTime? FechaGenerado { get; set; }

        [Column("fecha_pago")]
        public DateTime? FechaPago { get; set; }

        [Column("nota")]
        public string? Nota { get; set; }

        [Column("estado")]
        public int Estado { get; set; }


        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }

   
        public ICollection<FacturaPago>? Facturas { get; set; }
    }





    [Table("facturapago")]
    public class FacturaPago
    {
        [Column("id_factura")]
        public int Id { get; set; }

        [Column("id_pago")]
        public int IdPago { get; set; }

        [Column("fecha_factura")]
        public DateTime? FechaFactura { get; set; }

        [Column("monto_total")]
        public decimal MontoTotal { get; set; }

        [Column("NIT")]
        public string? Nit { get; set; }

        [Column("Detalle")]
        public string? Detalle { get; set; }

      
        [ForeignKey(nameof(IdPago))]
        public Pago? Pago { get; set; }

    }


    public class Cita
    {
        [Column("id_cita")]
        public int Id { get; set; }

        [Column("id_paciente")]
        public int IdPaciente { get; set; }

        [Column("id_medico")]
        public int? IdMedico { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("estado_cita")]
        public int EstadoCita { get; set; }

      
        [ForeignKey("IdPaciente")]
        public virtual Person? Paciente { get; set; }

        [ForeignKey("IdMedico")]
        public virtual User? Medico { get; set; }

   
        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }

    }

    public class FechaCita { public int Id { get; set; } public DateTime Fecha { get; set; } public int Estado { get; set; } }
    public class RegistroHorarioFecha { public int Id { get; set; } public int IdFecha { get; set; } public TimeSpan HoraInicio { get; set; } public TimeSpan HoraFin { get; set; } }

    public class Empleado
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Dpi { get; set; } = string.Empty;
        public string FormacionAcademica { get; set; } = string.Empty;
        public int? IdMunicipio { get; set; }
        public int? IdDepartamento { get; set; }
        public int Estado { get; set; } = 1;
    }

    public class Cargo { public int Id { get; set; } public string NombreCargo { get; set; } = ""; public string? Funcion { get; set; } public string? Requisito { get; set; } }

    [Table("bono")]
    public class Bono
    {
        [Key]
        [Column("id_bono")]
        public int IdBono { get; set; }

        [Column("id_medico")]
        public int? IdMedico { get; set; }

        [Column("id_persona")]
        public int? IdPersona { get; set; }

        [Column("nombre_bono")]
        public string NombreBono { get; set; } = string.Empty;

        [Column("monto_bono", TypeName = "decimal(12,2)")]
        public decimal MontoBono { get; set; }

        [Column("porcentaje", TypeName = "decimal(5,2)")]
        public decimal Porcentaje { get; set; }

        [Column("estado")]
        public int Estado { get; set; } = 1;

        [Column("pagado")]
        public bool Pagado { get; set; } = false;

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("fecha_pago")]
        public DateTime? FechaPago { get; set; }

      
        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }

        [ForeignKey(nameof(IdMedico))]
        [JsonIgnore] public User? Medico { get; set; }

        [ForeignKey(nameof(IdPersona))]
        [JsonIgnore] public Person? Persona { get; set; }
    }



    public class Descuento { public int Id { get; set; } public int IdEmpleado { get; set; } public decimal Monto { get; set; } public string? Descripcion { get; set; } }

    public class Cotizacion
    {
        public int Id { get; set; }

 
        public int IdExamen { get; set; }
        public Examen? Examen { get; set; }

        public decimal Precio { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

     
        public int? IdPago { get; set; }
        public Pago? Pago { get; set; }
    }


    public class Planilla { public int Id { get; set; } public int IdEmpleado { get; set; } public string? Puesto { get; set; } public int IdCargo { get; set; } public decimal SalarioBase { get; set; } public DateTime? FechaInicio { get; set; } public DateTime? FechaFin { get; set; } public string? NoCuenta { get; set; } public string? Banco { get; set; } }

    // ===== ENTIDADES DE PARÁMETROS =====

    public class Parametro
    {
        [Key]
        [Column("id_parametro")]
        public int Id { get; set; }

        [Column("id_tipo_examen")]
        public int IdTipoExamen { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("unidad")]
        public string? Unidad { get; set; }

        [Column("valor_referencia")]
        public string? ValorReferencia { get; set; }

        [ForeignKey(nameof(IdTipoExamen))]
        public TipoExamen? TipoExamen { get; set; }
    }

    public class ExamenParametro
    {
        public int Id { get; set; }
        public int IdExamen { get; set; }
        public int IdParametro { get; set; }
        public string? Valor { get; set; }

        public Examen? Examen { get; set; }
        public Parametro? Parametro { get; set; }
    }


    public class ParametroExamen
    {
        [Key]
        [Column("id_parametro_examen")]
        public int Id { get; set; }

        [Column("id_examen")]
        public int IdExamen { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("unidad")]
        public string? Unidad { get; set; }

        [Column("rango_referencia")]
        public string? RangoReferencia { get; set; }

        [Column("resultado")]
        public string? Resultado { get; set; }

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [ForeignKey(nameof(IdExamen))]
        public Examen? Examen { get; set; }
    }

    // =======================
    //  PARAMETRO TIPO EXAMEN
    // =======================
    public class ParametroTipoExamen
    {
        [Key]
        [Column("id_parametro_tipo_examen")]
        public int Id { get; set; }

        [Column("id_tipo_examen")]
        public int IdTipoExamen { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("unidad")]
        public string? Unidad { get; set; }

        [Column("rango_referencia")]
        public string? RangoReferencia { get; set; }

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        //  Relación con TipoExamen
        [ForeignKey("IdTipoExamen")]
        public TipoExamen? TipoExamen { get; set; }

        [Column("es_titulo")]
        public byte EsTitulo { get; set; } = 0; // 0 o 1

        //  Campo "orden" puede ser NULL en DB
        [Column("orden")]
        public int? Orden { get; set; } // Cambiado a nullable

        [NotMapped]
        public bool EsTituloBool
        {
            get => EsTitulo == 1;
            set => EsTitulo = (byte)(value ? 1 : 0);
        }
    }



    [Keyless]
    public class VistaExamenReporteClinico
    {
        public int IdExamen { get; set; }
        public int IdPaciente { get; set; }
        public string? Paciente { get; set; }
        public string? SexoPaciente { get; set; }
        public string? TelefonoPaciente { get; set; }
        public string? ReferidoPor { get; set; }
        public int IdTipoExamen { get; set; }
        public string? TipoExamen { get; set; }
        public string? DescripcionExamen { get; set; }
        public string? ResultadoGeneral { get; set; }
        public decimal? PrecioAplicado { get; set; }
        public int EstadoExamen { get; set; }
        public int? IdInsumo { get; set; }
        public int? IdParametroBase { get; set; }
        public string? ParametroBase { get; set; }
        public string? UnidadBase { get; set; }
        public string? ValorReferenciaBase { get; set; }
        public int? IdParametroPlantilla { get; set; }
        public string? ParametroPlantilla { get; set; }
        public string? UnidadPlantilla { get; set; }
        public string? RangoReferenciaPlantilla { get; set; }
        public string? ResultadoParametro { get; set; }
        public string? ObservacionesParametro { get; set; }
        public DateTime? FechaRegistro { get; set; }

     
        public DateTime? FechaNacimiento { get; set; }
        public int? Edad { get; set; }
    }



    [Keyless]
    public class TipoExamenView
    {
        public int Id_Tipo_Examen { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Precio { get; set; }
    }


    [Table("clinica")]
    public class Clinica
    {
        [Key]
        [Column("id_clinica")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("contacto")]
        public string? Contacto { get; set; }

        [Column("direccion")]
        public string? Direccion { get; set; }

        [Column("codigo")]
        public string? Codigo { get; set; }

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("activo")]
        public byte Activo { get; set; } = 1;

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }


        [NotMapped]
        public bool Estado
        {
            get => Activo == 1;
            set => Activo = (byte)(value ? 1 : 0);
        }
       
        [JsonIgnore]
        public ICollection<PrecioClinica> PreciosClinica { get; set; } = new List<PrecioClinica>();
    }



    public class PrecioClinica
    {
        [Key]
        [Column("id_precio_clinica")]
        public int Id { get; set; }

        [Column("id_clinica")]
        public int IdClinica { get; set; }

        [Column("id_tipo_examen")]
        public int IdTipoExamen { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }


        [Column("precio_especial", TypeName = "decimal(10,2)")]
        public decimal PrecioEspecial { get; set; }

        [Column("vigente_desde")]
        public DateTime? VigenteDesde { get; set; }

        [Column("vigente_hasta")]
        public DateTime? VigenteHasta { get; set; }

        [ForeignKey(nameof(IdClinica))]
        public Clinica? Clinica { get; set; }

        [ForeignKey(nameof(IdTipoExamen))]
        public TipoExamen? TipoExamen { get; set; }
    }
    [Table("perfil_examen")]
    public class PerfilExamen
    {
        [Key]
        [Column("id_perfil_examen")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("precio_total", TypeName = "decimal(10,2)")]
        public decimal PrecioTotal { get; set; } = 0;

        [Column("precio_paquete", TypeName = "decimal(10,2)")]
        public decimal PrecioPaquete { get; set; } = 0;

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }


        [Column("estado")]
        public bool Estado { get; set; } = true;

        [JsonIgnore]
        public ICollection<PerfilParametro>? PerfilParametros { get; set; }
    }


    [Table("perfil_parametro")]
    public class PerfilParametro
    {
        [Key]
        [Column("id_perfil_parametro")]
        public int Id { get; set; }

        [Column("id_perfil_examen")]
        public int IdPerfilExamen { get; set; }

        [Column("id_tipo_examen")]
        public int IdTipoExamen { get; set; }

        [ForeignKey(nameof(IdPerfilExamen))]
        public PerfilExamen? PerfilExamen { get; set; }

        [ForeignKey(nameof(IdTipoExamen))]
        public TipoExamen? TipoExamen { get; set; }
    }

    [Table("tipo_examen_insumo")]
    public class TipoExamenInsumo
    {
        [Key]
        [Column("id")] 
        public int Id { get; set; }

        [Column("id_tipo_examen")]
        public int IdTipoExamen { get; set; }

        [Column("id_insumo")]
        public int IdInsumo { get; set; }

        [Column("cantidad_usada", TypeName = "decimal(10,2)")]
        public decimal CantidadUsada { get; set; } = 1;

        [ForeignKey(nameof(IdTipoExamen))]
        public TipoExamen? TipoExamen { get; set; }

        [ForeignKey(nameof(IdInsumo))]
        public Insumo? Insumo { get; set; }
    }

    [Table("registro_insumo_uso")]
    public class RegistroInsumoUso
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_insumo")]
        public int IdInsumo { get; set; }

        [Column("id_examen")]
        public int? IdExamen { get; set; }

        [Column("cantidad_usada", TypeName = "decimal(10,2)")]
        public decimal CantidadUsada { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column("justificacion")]
        public string? Justificacion { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [ForeignKey(nameof(IdInsumo))]
        public Insumo? Insumo { get; set; }

        [ForeignKey(nameof(IdExamen))]
        public Examen? Examen { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }
    }

}
