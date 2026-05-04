using DevExpress.Blazor;
using DevExpress.Data.Filtering;
using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class Empleados
	{
		private List<EmpleadoModel> Lista = new();
		private EmpleadoModel Modelo = new();
		private EditContext? editContext;
		private bool cargando = true;
		private bool mostrarFormulario = false;

		// Catálogos
		private List<DeptoItem> DeptosList = new();
		private List<PuestoItem> PuestosList = new();
		private List<PuestoItem> PuestosListAux = new();
		private List<TurnoItem> TurnosList = new();
		private List<CatalogoSatModel> TipoRegimenList = new();
		private List<CatalogoSatModel> TipoContratoList = new();
		private List<CatalogoSatModel> TipoJornadaList = new();
		private List<CatalogoSatModel> BancosList = new();

		private List<dynamic> sexoOpciones = new()
		{
			new { Valor = 'H', Texto = "Hombre" },
			new { Valor = 'M', Texto = "Mujer"  }
		};

		private List<dynamic> estadoCivilOpciones = new()
		{
			new { Valor = "Soltero",   Texto = "Soltero(a)"   },
			new { Valor = "Casado",    Texto = "Casado(a)"    },
			new { Valor = "Divorciado",Texto = "Divorciado(a)"},
			new { Valor = "Viudo",     Texto = "Viudo(a)"     },
			new { Valor = "Unión Libre",Texto = "Unión Libre" }
		};

		DxWindow WindowRef;
		int ventanaX;
		int ventanaY;

		protected override async Task OnInitializedAsync()
		{
			await Task.WhenAll(
				CargarLista(),
				CargarCatalogos()
			);
		}

		
		private async Task CargarLista()
		{
			cargando = true;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Empleados/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<EmpleadoModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Lista = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
			finally
			{
				cargando = false;
				StateHasChanged();
			}
		}

		private async Task CargarCatalogos()
		{
			await Task.WhenAll(
				CargarDepto(),
				CargarPuestos(),
				CargarTurnos(),
				CargarSat("api/CatalogosSat/TipoRegimen", r => TipoRegimenList = r),
				CargarSat("api/CatalogosSat/TipoContrato", r => TipoContratoList = r),
				CargarSat("api/CatalogosSat/TipoJornada", r => TipoJornadaList = r),
				CargarSat("api/CatalogosSat/Bancos", r => BancosList = r)
			);
		}

		private async Task CargarDepto()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				var request = new HttpRequestMessage(HttpMethod.Get, "api/Departamentos/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<DeptoItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					DeptosList = resultado?.Data ?? new();
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task CargarPuestos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				var request = new HttpRequestMessage(HttpMethod.Get, "api/Puesto/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<PuestoItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					PuestosList = resultado?.Data ?? new();
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task CargarTurnos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				var request = new HttpRequestMessage(HttpMethod.Get, "api/Turno/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<TurnoItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					TurnosList = resultado?.Data ?? new();
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task CargarSat(string url, Action<List<CatalogoSatModel>> asignar)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				var request = new HttpRequestMessage(HttpMethod.Get, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<CatalogoSatModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					asignar(resultado?.Data ?? new());
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task OnDepartamentoChanged(int departamentoId)
		{
			Modelo.IdDepartamento = departamentoId;

			Modelo.IdPuesto = 0;

			if (departamentoId > 0)
			{
				await CargaPuestosPorDepartamento(departamentoId);
			}
			else
			{
				PuestosListAux = new(); // Si limpian el combo, vaciamos la lista de puestos
			}
		}

		private async Task CargaPuestosPorDepartamento(int departamentoId)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) return;

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/Puesto/ObtenerPorDepartamento/{departamentoId}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<PuestoItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					PuestosListAux = resultado?.Data ?? new();
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error al cargar puestos: " + ex.Message);
			}
			finally
			{
				StateHasChanged();
			}
		}
		private async Task AbrirFormulario(EmpleadoModel? item = null)
		{
			if (item == null)
			{
				Modelo = new EmpleadoModel
				{
					Id = 0,
					Paterno = "",
					Materno = "",
					Nombre = "",
					Rfc = "",
					Curp = "",
					Nss = "",
					Sexo = 'H',
					FechaNacimientoDt = DateTime.Today.AddYears(-18),
					FechaIngresoDt = DateTime.Today
				};
			}
			else
			{
				Modelo = new EmpleadoModel
				{
					Id = item.Id,
					Paterno = item.Paterno,
					Materno = item.Materno,
					Nombre = item.Nombre,
					Rfc = item.Rfc,
					Curp = item.Curp,
					Nss = item.Nss,
					IdDepartamento = item.IdDepartamento,
					IdPuesto = item.IdPuesto,
					IdTurno = item.IdTurno,
					IdTipoRegimen = item.IdTipoRegimen,
					IdTipoContrato = item.IdTipoContrato,
					IdTipoJornada = item.IdTipoJornada,
					Sexo = item.Sexo,
					EstadoCivil = item.EstadoCivil,
					FechaNacimientoDt = item.FechaNacimientoDt,
					FechaIngresoDt = item.FechaIngresoDt,
					Calle = item.Calle,
					Cp = item.Cp,
					Colonia = item.Colonia,
					Municipio = item.Municipio,
					Localidad = item.Localidad,
					Telefono = item.Telefono,
					Correo = item.Correo,
					NoCuenta = item.NoCuenta,
					IdBanco = item.IdBanco,
					Observaciones = item.Observaciones
				};
			}

			editContext = new EditContext(Modelo);
			if (item!=null)
				await CargaPuestosPorDepartamento(item.IdDepartamento);

			int anchoPantalla = await JS.InvokeAsync<int>("eval", "window.innerWidth");

			// 2. Calculamos el centro: (Mitad de pantalla) - (Mitad del modal)
			// Si el modal mide 600px, restamos 300.
			ventanaX = (anchoPantalla / 2) - 325;
			ventanaY = 100; // Una altura cómoda desde el techo
			mostrarFormulario = true;
			
			StateHasChanged();
		}

		private async Task CerrarFormulario()
		{
			mostrarFormulario = false;
			await InvokeAsync(StateHasChanged);
		}

		private async Task Guardar()
		{
			if (editContext == null || !editContext.Validate()) return;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var url = Modelo.Id == 0
					? "api/Empleados/Agregar"
					: $"api/Empleados/Editar/{Modelo.Id}";

				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(Modelo);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success",
						Modelo.Id == 0 ? "Empleado agregado exitosamente." : "Empleado actualizado correctamente.");
					mostrarFormulario = false;
					await CargarLista();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo guardar: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		//este método es propio del DxGrid, este funciona para acceder a propiedades dentro del mismo y poder editar cosas de él (en este caso
		//lo estoy utilizando para poner un width: 100% al Searchbox, se pueden editar más cosas de css e incluso asignarle clases)
		void Grid_CustomizeElement(GridCustomizeElementEventArgs e)
		{
			if (e.ElementType == GridElementType.SearchBoxContainer)
			{
				e.Style = "Width: 100%;";


			}
		}

		public class DeptoItem
		{
			public int Id { get; set; }
			public string Nombre { get; set; } = string.Empty;
		}

		public class PuestoItem
		{
			public int Id { get; set; }
			public string Nombre { get; set; } = string.Empty;
		}

		public class TurnoItem
		{
			public int Id { get; set; }
			public string Descripcion { get; set; } = string.Empty;
		}

	}
}

