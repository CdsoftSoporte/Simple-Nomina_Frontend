using DevExpress.XtraPrinting;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Departamentos;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Puestos;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class Turnos
	{

		private List<Turno> TurnosList = [];
		private Turno TurnosModel = new();
		private bool isModalVisible = false;
		private EditContext? editContext;
		string DisplayFormat { get; } = string.IsNullOrEmpty(CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator) ? "HH:mm" : "h:mm tt";

		private List<dynamic> estatusOpciones = new()
		{
			new { Valor = 'S', Texto = "Activo" },
			new { Valor = 'N', Texto = "Inactivo" }
		};

		public class DiaSemana
		{
			public string Valor { get; set; } = string.Empty;
			public string Texto { get; set; } = string.Empty;
			public bool Marcado { get; set; } = false;
		}
		private List<DiaSemana> diasSemana = new()
		{
			new DiaSemana{Valor="Lun",Texto="Lun"},
			new DiaSemana{Valor="Mar",Texto="Mar"},
			new DiaSemana{Valor="Mié",Texto="Mié"},
			new DiaSemana{Valor="Jue",Texto="Jue"},
			new DiaSemana{Valor="Vie",Texto="Vie"},
			new DiaSemana{Valor="Sáb",Texto="Sáb"},
			new DiaSemana{Valor="Dom",Texto="Dom"},
		};

		private bool cargando = true;

		protected override async Task OnInitializedAsync()
		{
			await CargarTurnos();
		}

		private async Task CargarTurnos()
		{
			cargando = true;
			try
			{
				
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Turno/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<Turno>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					TurnosList = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudieron cargar los turnos: " + (errorData?.Message ?? "Error inesperado")+errorData.Details);
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

		private void MostrarModal(Turno? item = null)
		{
			if (item == null)
			{
				TurnosModel = new Turno
				{
					Id = 0,
					Descripcion = "",
					hora_entrada= new TimeOnly(0,0,0),
					hora_salida= new TimeOnly(0,0,0),
					dias_descanso="",
					Activo = 'S',

				};
			}
			else
			{
				TurnosModel = new Turno
				{
					Id = item.Id,
					Descripcion = item.Descripcion,
					hora_entrada = item.hora_entrada,
					hora_salida = item.hora_salida,
					dias_descanso = item.dias_descanso,
					Activo = item.Activo

					
				};
			}

			if (item != null) {
				MarcarDiasDescanso(item.dias_descanso);
			}
			else
			{
				
				foreach (var dia in diasSemana)
					dia.Marcado = false;
				
			}

			editContext = new EditContext(TurnosModel);
			isModalVisible = true;
			StateHasChanged();
		}

		private void OnDiaDescansoChanged(DiaSemana dia, bool valor)
		{
			dia.Marcado = valor;

			TurnosModel.dias_descanso = string.Join(",",
				diasSemana
					.Where(d => d.Marcado)
					.Select(d => d.Valor));
		}

		private void MarcarDiasDescanso(string dias_descanso)
		{
			var seleccionados = string.IsNullOrEmpty(dias_descanso)
				? new List<string>()
				: dias_descanso.Split(',').ToList();

			foreach (var dia in diasSemana)
				dia.Marcado = seleccionados.Contains(dia.Valor);
		}

		private async Task Guardar()
		{
			if (editContext == null || !editContext.Validate()) return;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				HttpRequestMessage request;

				if (TurnosModel.Id == 0)
				{
					request = new HttpRequestMessage(HttpMethod.Post, "api/Turno/Agregar");
				}
				else
				{
					request = new HttpRequestMessage(HttpMethod.Post, $"api/Turno/Editar/{TurnosModel.Id}");
				}

				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(TurnosModel);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success", TurnosModel.Id == 0 ? "Turno agregado exitosamente." : "Turno actualizado correctamente.");
					isModalVisible = false;
					await CargarTurnos();
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



		public class Turno
		{
	
				public int Id { get; set; }

				[Required(ErrorMessage = "La descripción es obligatoria")]
				[StringLength(100, ErrorMessage = "Máximo 255 caracteres")]
				public string? Descripcion { get; set; }

				public TimeOnly hora_entrada { get; set; }
				public TimeOnly hora_salida { get; set; }

			public string dias_descanso { get; set; }

				public char? Activo { get; set; }

				[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
				public DateTime? Fecha_Creacion { get; set; }

				public DateTime? Fecha_Actualizacion { get; set; }
			
		}























	}
}
