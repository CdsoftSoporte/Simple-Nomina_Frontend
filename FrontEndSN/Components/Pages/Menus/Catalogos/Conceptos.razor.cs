using DevExpress.Blazor;
using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class Conceptos : VentanaBase
	{
		private bool cargando = false;
		private ConceptoModel Modelo = new ConceptoModel();
		private List<ConceptoModel> ListaConceptos;
		private EditContext editContext;
		private bool esEdicion = false;
		private bool mostrarModal = false;

		private List<ItemCatalogoString> catalogoTipos;
		private List<ItemCatalogoInt> catalogoBases;
		private List<ItemCatalogoInt> catalogoExenciones;
		private List<CatalogoSatModel> catalogoSat;
		DxWindow WindowRef;


		public bool GravadoIsrBool
		{
			get => Modelo.GravadoIsr == 'S';
			set => Modelo.GravadoIsr = value ? 'S' : 'N';
		}

		// Propiedad para Activo
		public bool ActivoBool
		{
			get => Modelo.Activo == 'S';
			set => Modelo.Activo = value ? 'S' : 'N';
		}

		// Propiedad para Afecta Neto
		public bool AfectaNetoBool
		{
			get => Modelo.AfectaNeto == 'S';
			set => Modelo.AfectaNeto = value ? 'S' : 'N';
		}

		// Propiedad para Integra SBC
		public bool IntegraSBCBool
		{
			get => Modelo.IntegraSBC == 'S';
			set => Modelo.IntegraSBC = value ? 'S' : 'N';
		}

		// Propiedad para Visible en Recibo
		public bool VisibleEnReciboBool
		{
			get => Modelo.VisibleEnRecibo == 'S';
			set => Modelo.VisibleEnRecibo = value ? 'S' : 'N';
		}

		protected override async Task OnInitializedAsync()
		{
			await Task.WhenAll(
				CargarConceptos(),
				CargarSat("api/CatalogosSat/Conceptos", r => catalogoSat = r),
				CargarCatalogosLocal()
			);
		}


		private void CerrarFormulario()
		{
			mostrarModal = false;
			Modelo = new ConceptoModel();
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

		private async Task GuardarConcepto()
		{
			// Lógica para enviar a tu API/Base de datos
			// IMPORTANTE: Aquí mapearías los "bool" (AfectaNeto) a los chars 'S'/'N' de tu entidad real

			await CargarConceptos(); // Refrescar Grid
			CerrarFormulario();
		}

		private async Task CargarCatalogosLocal()
		{
			catalogoTipos = new List<ItemCatalogoString>
			{
				new() { Clave = 'P', Nombre = "Percepción" },
				new() { Clave = 'D', Nombre = "Deducción" },
				new() { Clave = 'U', Nombre = "Unidades" },
				new() { Clave = 'T', Nombre = "Totales" },
				new() { Clave = 'O', Nombre = "Otro Pago" }
			};

			catalogoBases = new List<ItemCatalogoInt>
			{
				new() { Id = 1, Nombre = "1 - Importe Fijo" },
				new() { Id = 2, Nombre = "2 - % del Sueldo Ordinario" },
				new() { Id = 3, Nombre = "3 - Múltiplo de UMA" },
				new() { Id = 4, Nombre = "4 - Múltiplo de Salario Mínimo" },
				new() { Id = 5, Nombre = "5 - Días de Sueldo" }
			};

			catalogoExenciones = new List<ItemCatalogoInt>
			{
				new() { Id = 0, Nombre = "0 - Totalmente Gravado" },
				new() { Id = 1, Nombre = "1 - Exento en base a UMA" },
				new() { Id = 2, Nombre = "2 - Exento en base a SMG" },
				new() { Id = 3, Nombre = "3 - % del Importe Total" },
				new() { Id = 4, Nombre = "4 - Importe Fijo Exento" }
			};

		}


		private async Task CargarConceptos()
		{
			cargando = true;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Conceptos/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<ConceptoModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaConceptos = resultado?.Data ?? new();
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



		private async Task AbrirFormulario(ConceptoModel? item = null)
		{
			if (item == null)
			{
				Modelo = new ConceptoModel
				{
					Id = 0,
					Descripcion="",
					Tipo='P',
					EsSistema='N',
					GravadoIsr='S',
					ExentoTipo=0,
					ExentoLimite=0,
					IntegraSBC='N',
					AfectaNeto='S',
					VisibleEnRecibo='S',
					BaseCalculo=1,
					Valorcalculo=0,
					Activo='S',
					IdConceptoSat=0
				

				};
			}
			else
			{
				Modelo = item;
			}

			editContext = new EditContext(Modelo);
			await PrepararVentana(600);
			mostrarModal = true;
			StateHasChanged();
		}

		private string ObtenerNombreTipo(char clave)
		{
			return catalogoTipos.FirstOrDefault(x => x.Clave == clave)?.Nombre ?? clave.ToString();
		}



	}



		public class ConceptoModel
		{
			public int Id { get; set; }
			public string Descripcion { get; set; }
			public char Tipo { get; set; }

			[JsonPropertyName("es_sistema")]
			public char EsSistema { get; set; }

			[JsonPropertyName("gravado_isr")]
			public char GravadoIsr { get; set; }

			[JsonPropertyName("exento_tipo")]
			public int? ExentoTipo { get; set; }

			[JsonPropertyName("exento_limite")]
			public decimal? ExentoLimite { get; set; }

			[JsonPropertyName("integra_sbc")]
			public char IntegraSBC { get; set; }

			[JsonPropertyName("afecta_neto")]
			public char AfectaNeto { get; set; }

			[JsonPropertyName("visible_en_recibo")]
			public char VisibleEnRecibo { get; set; }

			[JsonPropertyName("base_calculo")]
			public int BaseCalculo { get; set; }

			[JsonPropertyName("valor_calculo")]
			public decimal Valorcalculo { get; set; }
			public char Activo { get; set; }

			[JsonPropertyName("id_concepto_sat")]
			public int? IdConceptoSat { get; set; }


		}

	public class ItemCatalogoString { public char Clave { get; set; } public string Nombre { get; set; } }
	public class ItemCatalogoInt { public int Id { get; set; } public string Nombre { get; set; } }


}

