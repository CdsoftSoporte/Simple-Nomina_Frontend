using DevExpress.Blazor;
using DevExpress.Data.Filtering;
using DevExpress.DocumentView;
using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Catalogos.ConfiguracionEmpresa;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Departamentos;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Uma;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class Empleados:VentanaBase
	{
		private List<EmpleadoModel> Lista = new();
		private List<ExpendienteEmpleados> Expediente = new();
		private EmpleadoModel Modelo = new();
		private EditContext? editContext;
		private bool cargando = true;
		private bool mostrarFormulario = false;
		private bool TieneArchivos = false;

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
		private int IdCentroDeCostosSeleccionado = 0;

		private string? nuevoDocumentoNombre;
		private DateOnly? nuevoDocumentoCaducidad;


		protected override async Task OnInitializedAsync()
		{
			await Task.WhenAll(
				CargarLista(),
				CargarCatalogos()
			);
		}

		private async Task SubirArchivoExpediente(InputFileChangeEventArgs e)
		{
			try
			{
				// 1. Validar que el usuario haya puesto un nombre al documento antes de enviarlo
				if (string.IsNullOrWhiteSpace(nuevoDocumentoNombre))
				{
					await JS.InvokeVoidAsync("notifier.error", "Por favor, ingresa el nombre del documento.");
					return;
				}

				StateHasChanged();

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var archivo = e.File;
				var contenido = new MultipartFormDataContent();

				//  Agregar el Archivo
				var streamContent = new StreamContent(archivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)); // Límite 10MB
				contenido.Add(streamContent, "archivo", archivo.Name);

				//  Agregar los campos de texto
				contenido.Add(new StringContent(nuevoDocumentoNombre), "nombre_documento");

				//  Agregar la fecha solo si el usuario seleccionó una
				if (nuevoDocumentoCaducidad.HasValue)
				{
					// La mandamos en formato ISO para que el backend la entienda perfectamente
					contenido.Add(new StringContent(nuevoDocumentoCaducidad.Value.ToString("yyyy-MM-dd")), "fecha_caducidad");
				}


				var request = new HttpRequestMessage(HttpMethod.Post, $"api/ExpedientesEmpleados/subir/{Modelo.Id}");

				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = contenido;

				// 6. Enviar
				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{


					await JS.InvokeVoidAsync("notifier.success", "Archivo guardado correctamente.");

					nuevoDocumentoNombre = string.Empty;
					nuevoDocumentoCaducidad = null;
					await TraerExpedienteEmpleado(Modelo.Id);
					Modelo.Expediente = Expediente;


				}
				else
				{
					// Extraer el mensaje de error del backend para mostrarlo
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error: " + ex.Message);
			}
			finally
			{
				StateHasChanged();
			}
		}
		
		private async Task DescargarExpediente()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				// Apuntamos al nuevo endpoint de ZIP
				var request = new HttpRequestMessage(HttpMethod.Get, $"api/ExpedientesEmpleados/descargar_expediente/{Modelo.Id}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var fileStream = await response.Content.ReadAsStreamAsync();
					using var streamRef = new DotNetStreamReference(stream: fileStream);

					// Le decimos a JS que lo guarde como ZIP
					await JS.InvokeVoidAsync("DescargarArchivos", streamRef, $"Expediente_{Modelo.Paterno}_{Modelo.Materno}_{Modelo.Nombre}.zip");
				}
				else
				{
					await JS.InvokeVoidAsync("notifier.error", "Empleado sin documentos en su expediente");
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error: " + ex.Message);
			}
		}

		private async Task DescargarDocumento(int id_documento,string extension)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				// 1. Configuramos la petición GET (Asegúrate de que la ruta coincida con tu endpoint)
				var request = new HttpRequestMessage(HttpMethod.Get, $"api/ExpedientesEmpleados/descargar/{id_documento}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				// 2. Enviamos la petición
				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					// 3. Leemos el archivo como Stream
					var fileStream = await response.Content.ReadAsStreamAsync();
					using var streamRef = new DotNetStreamReference(stream: fileStream);

					// 4. Determinamos el MIME Type en el frontend para que JS sepa cómo dibujarlo
					string mimeType = extension.ToLower() switch
					{
						".pdf" => "application/pdf",
						".jpg" => "image/jpeg",
						".jpeg" => "image/jpeg",
						".png" => "image/png",
						".doc" => "application/msword",
						".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
						".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
						".xls" => "application/vnd.ms-excel",
						_ => "application/octet-stream"
					};

					// 5. Llamamos a nuestra nueva función de JavaScript
					await JS.InvokeVoidAsync("abrirArchivoEnNuevaPestana", streamRef, mimeType);
				}
				else
				{
					await JS.InvokeVoidAsync("notifier.error", "No se pudo obtener el archivo.");
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error: " + ex.Message);
			}
		}

		private async Task ConfirmarEliminar(ExpendienteEmpleados item)
		{
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
			"¿Eliminar Registro?",
			 $"¿Deseas borrar el documento {item.NombreDocumento} del expediente?");
			if (confirmed)
			{
				await EliminarDocumento(item.Id);
				await TraerExpedienteEmpleado(item.IdEmpleado);
				Modelo.Expediente = Expediente;

			}
			StateHasChanged();
		}

		private async Task EliminarDocumento(int id)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				var request = new HttpRequestMessage(HttpMethod.Delete, $"api/ExpedientesEmpleados/borrar/{id}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{

					if (response.IsSuccessStatusCode)
					{
						await JS.InvokeVoidAsync("notifier.success", "Registro eliminado correctamente");
					}
					else
					{
						await JS.InvokeVoidAsync("notifier.error", response.Content);
					}

				}
				else
				{

					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();

					string mensajePrincipal = errorData?.Message ?? "Error inesperado";
					string detalleTecnico = errorData?.Details ?? "No hay detalles adicionales.";
					await JS.InvokeVoidAsync("notifier.error", "No se pudo guardar la información: " + mensajePrincipal + " " + detalleTecnico);

				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", $"Ocurrió un error: " + ex.InnerException);
			}
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
				CargarCentrosDeCostosParaCombo(),
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

		private async Task TraerExpedienteEmpleado(int id_empleado)
		{
			Expediente.Clear();
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) return;

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/ExpedientesEmpleados/listar/{id_empleado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<ExpendienteEmpleados>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Expediente = resultado?.Data ?? new();
				}

				TieneArchivos =(Expediente.Count>0?true:false);

			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error al cargar Expediente: " + ex.Message);
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
				 await TraerExpedienteEmpleado(item.Id);

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
					IdCentroCostos=item.IdCentroCostos,
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
					Observaciones = item.Observaciones,
					Expediente = Expediente
				};
			}

			editContext = new EditContext(Modelo);
			if (item!=null)
				await CargaPuestosPorDepartamento(item.IdDepartamento);

			int anchoPantalla = await JS.InvokeAsync<int>("eval", "window.innerWidth");

			//// 2. Calculamos el centro: (Mitad de pantalla) - (Mitad del modal)
			//// Si el modal mide 600px, restamos 300.
			ventanaX = (anchoPantalla / 2) - 100;
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

		protected async Task OnCentroCostosChanged(int nuevoCentro)
		{
			IdCentroDeCostosSeleccionado = nuevoCentro;
			StateHasChanged();
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

