using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FrontEndSN.Clases
{
	public class EmpleadoModel
	{

		public int Id { get; set; }

		[Required(ErrorMessage = "El apellido paterno es obligatorio")]
		public string Paterno { get; set; } = string.Empty;

		public string? Materno { get; set; }

		[Required(ErrorMessage = "El nombre es obligatorio")]
		public string? Nombre { get; set; }

		[Required(ErrorMessage = "El RFC es obligatorio")]
		[StringLength(13, MinimumLength = 12, ErrorMessage = "RFC debe tener entre 12 y 13 caracteres")]
		public string Rfc { get; set; } = string.Empty;

		[Required(ErrorMessage = "El CURP es obligatorio")]
		[StringLength(18, MinimumLength = 18, ErrorMessage = "El CURP debe tener 18 caracteres")]
		public string Curp { get; set; } = string.Empty;

		[Required(ErrorMessage = "El NSS es obligatorio")]
		[StringLength(11, MinimumLength = 11, ErrorMessage = "El NSS debe tener 11 dígitos")]
		public string Nss { get; set; } = string.Empty;

		[Range(1, int.MaxValue, ErrorMessage = "Seleccione un departamento")]
		[JsonPropertyName("id_departamento")]
		public int IdDepartamento { get; set; }

		[Range(1, int.MaxValue, ErrorMessage = "Seleccione un puesto")]
		[JsonPropertyName("id_puesto")]
		public int IdPuesto { get; set; }

		[Range(1, int.MaxValue, ErrorMessage = "Seleccione un turno")]
		[JsonPropertyName("id_turno")]
		public int IdTurno { get; set; }

		[JsonPropertyName("id_tipo_regimen")]
		public int? IdTipoRegimen { get; set; }

		[JsonPropertyName("id_tipo_contrato")]
		public int? IdTipoContrato { get; set; }

		[JsonPropertyName("id_tipo_jornada")]
		public int? IdTipoJornada { get; set; }

		public char Sexo { get; set; } = 'H';

		[JsonPropertyName("estado_civil")]
		public string? EstadoCivil { get; set; }

		// Auxiliares DateTime para DxDateEdit
		[Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
		[JsonIgnore]
		public DateTime FechaNacimientoDt { get; set; } = DateTime.Today.AddYears(-18);

		[JsonIgnore]
		public DateTime FechaIngresoDt { get; set; } = DateTime.Today;

		// Propiedades que se envían al backend como DateOnly
		[JsonPropertyName("fecha_nacimiento")]
		public DateOnly FechaNacimiento
		{
			get => DateOnly.FromDateTime(FechaNacimientoDt);
			set => FechaNacimientoDt = value.ToDateTime(TimeOnly.MinValue);
		}

		[JsonPropertyName("fecha_ingreso")]
		public DateOnly? FechaIngreso
		{
			get => DateOnly.FromDateTime(FechaIngresoDt);
			set => FechaIngresoDt = value?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
		}

		public string? Calle { get; set; }
		public string? Cp { get; set; }
		public string? Colonia { get; set; }
		public string? Municipio { get; set; }
		public string? Localidad { get; set; }

		[JsonPropertyName("no_cuenta")]
		public string? NoCuenta { get; set; }

		public string? Telefono { get; set; }
		public string? Correo { get; set; }
		public string? Observaciones { get; set; }

		[JsonPropertyName("id_banco")]
		public int? IdBanco { get; set; }

		public string NombreCompleto => $"{Paterno} {Materno} {Nombre}".Trim();

	}

	public class EmpleadoDTO 
	{
		public int Id { get; set; }

		public string Paterno { get; set; } = string.Empty;

		public string? Materno { get; set; }

		public string? Nombre { get; set; }

		public string NombreCompleto => $"{Paterno} {Materno} {Nombre}".Trim();
	}

}
