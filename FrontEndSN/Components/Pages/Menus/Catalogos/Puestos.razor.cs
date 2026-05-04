using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Departamentos;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class Puestos
	{
		private List<Puesto> PuestosList = [];
		private List<Departamento> DeptosList = [];
		private Puesto PuestoModel = new();
		private bool isModalVisible = false;
		private EditContext? editContext;

		private List<dynamic> estatusOpciones = new()
		{
			new { Valor = 'S', Texto = "Activo" },
			new { Valor = 'N', Texto = "Inactivo" }
		};

		protected override async Task OnInitializedAsync()
		{
			await CargarDepartamentos();
			await CargarPuestos();
		}

		private async Task CargarPuestos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Puesto/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<Puesto>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					PuestosList = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudieron cargar los puestos: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
			finally
			{
				StateHasChanged();
			}
		}

		private async Task CargarDepartamentos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Departamentos/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<Departamento>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					DeptosList = resultado?.Data ?? new();
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private void MostrarModal(Puesto? item = null)
		{
			if (item == null)
			{
				PuestoModel = new Puesto
				{
					Id = 0,
					Nombre = "",
					Descripcion = "",
					Activo = 'S',
					SalarioBase = 0
				};
			}
			else
			{
				PuestoModel = new Puesto
				{
					Id = item.Id,
					Nombre = item.Nombre,
					Descripcion = item.Descripcion,
					IdDepartamento = item.IdDepartamento,
					SalarioBase = item.SalarioBase,
					Activo = item.Activo
				};
			}

			editContext = new EditContext(PuestoModel);
			isModalVisible = true;
			StateHasChanged();
		}

		private async Task Guardar()
		{
			if (editContext == null || !editContext.Validate()) return;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				HttpRequestMessage request;

				if (PuestoModel.Id == 0)
				{
					request = new HttpRequestMessage(HttpMethod.Post, "api/Puesto/Agregar");
				}
				else
				{
					request = new HttpRequestMessage(HttpMethod.Post, $"api/Puesto/Editar/{PuestoModel.Id}");
				}

				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(PuestoModel);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success", PuestoModel.Id == 0 ? "Puesto agregado exitosamente." : "Puesto actualizado correctamente.");
					isModalVisible = false;
					await CargarPuestos();
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

		public class Puesto
		{
			public int Id { get; set; }

			[Required(ErrorMessage = "El nombre es obligatorio")]
			[StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
			public string Nombre { get; set; } = string.Empty;

			[Required(ErrorMessage = "La descripcion es obligatoria")]
			[StringLength(100, ErrorMessage = "Máximo 255 caracteres")]
			public string? Descripcion { get; set; }

			[Range(1, int.MaxValue, ErrorMessage = "Seleccione un departamento")]
			[JsonPropertyName("id_departamento")]
			public int IdDepartamento { get; set; }

			[Range(0, double.MaxValue, ErrorMessage = "El salario debe ser mayor o igual a 0")]
			[JsonPropertyName("salario_base")]
			public decimal SalarioBase { get; set; } = 0;

			public char? Activo { get; set; } = 'S';

			[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
			public DateTime? Fecha_Creacion { get; set; }

			public DateTime? Fecha_Actualizacion { get; set; }
		}
	}
}