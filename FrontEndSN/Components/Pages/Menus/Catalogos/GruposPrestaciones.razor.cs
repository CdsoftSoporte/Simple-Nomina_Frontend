
using Azure;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Uma;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class GruposPrestaciones
	{
		private List<PrestacionLey> listaGrupos = new();
		private PrestacionLey? grupoSeleccionado;
		private PrestacionLey? grupoModel;

		private List<dynamic> estatusOpciones = new() {
			new { Valor = "S", Texto = "Activo" },
			new { Valor = "N", Texto = "Inactivo" }
		};

		protected override async Task OnInitializedAsync()
		{
			await CargarGrupos();
		}

		private async Task CargarGrupos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				if (string.IsNullOrEmpty(token))
				{

					Console.WriteLine("No se encontró el token de autenticación.");
					Nav.NavigateTo("/login");
					return;
				}

				var request = new HttpRequestMessage(HttpMethod.Get, "api/GruposPrestaciones/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<PrestacionLey>>>(
									new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
					);
					listaGrupos = resultado.Data ?? new();

				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();

					string mensajePrincipal = errorData?.Message ?? "Error inesperado";
					string detalleTecnico = errorData?.Details ?? "No hay detalles adicionales.";
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la información: " + mensajePrincipal + " " + detalleTecnico);
				}
			}
			catch (Exception ex)
			{

				await JS.InvokeVoidAsync("notifier.error", $"Ocurrió un error: " + ex.Message);
			}
			finally
			{

				StateHasChanged();
			}
		}

		private void OnGrupoChanged(PrestacionLey g)
		{
			grupoSeleccionado = g;
			if (g == null)
			{
				grupoModel = null;
				StateHasChanged();
				return;
			}

			grupoModel = new PrestacionLey
			{
				Id = g.Id,
				Descripcion = g.Descripcion,
				Activo = g.Activo,
				Detalles = g.Detalles.Select(d => new PrestacionLeyDetalle
				{
					Id = d.Id,
					Idgrupo_prestacion = d.Idgrupo_prestacion,
					Anos = d.Anos,
					DiasVacaciones = d.DiasVacaciones,
					DiasAguinaldo = d.DiasAguinaldo,
					PrimaVacacional = d.PrimaVacacional,
					FactorIntegracion = d.FactorIntegracion
				}).ToList()
			};

			StateHasChanged();
		}

		private void PrepararNuevoGrupo()
		{
			grupoSeleccionado = null;
			grupoModel = new PrestacionLey
			{
				Id = 0,
				Descripcion = "",
				Activo = "S",
				Detalles = new List<PrestacionLeyDetalle>()
			};
		}

		private void AgregarRenglon()
		{
			if (grupoModel != null)
			{
				int siguienteAno = grupoModel.Detalles.Any() ? grupoModel.Detalles.Max(x => x.Anos) + 1 : 1;

				grupoModel.Detalles.Add(new PrestacionLeyDetalle
				{
					Anos = siguienteAno,
					DiasAguinaldo = 15,
					PrimaVacacional = 25,
					DiasVacaciones = 12,
					FactorIntegracion = 1.0493m // Valor base de ley
				});
			}
		}

		private async Task OnEditModelSaving(GridEditModelSavingEventArgs e)
		{
			var editModel = (PrestacionLeyDetalle)e.EditModel;
			var dataItem = (PrestacionLeyDetalle)e.DataItem;

			// Lógica de cálculo automático (Basado en Ley)
			decimal primaProporcional = editModel.PrimaVacacional / 100m;
			decimal factorCalculado = 1 + (((editModel.DiasVacaciones * primaProporcional) + editModel.DiasAguinaldo) / 365m);

			editModel.FactorIntegracion = Math.Round(factorCalculado, 6);

			if (e.IsNew)
			{
				grupoModel.Detalles.Add(editModel);
			}
			else
			{
				// Actualizamos los valores en la lista
				dataItem.Anos = editModel.Anos;
				dataItem.DiasVacaciones     = editModel.DiasVacaciones;
				dataItem.DiasAguinaldo      = editModel.DiasAguinaldo;
				dataItem.PrimaVacacional    = editModel.PrimaVacacional;
				dataItem.FactorIntegracion  = editModel.FactorIntegracion;
			}

			await InvokeAsync(StateHasChanged);
		}


		private void RecalcularFactor(PrestacionLeyDetalle model, string campoModificado, object nuevoValor)
		{
			switch (campoModificado)
			{
				case "DiasVacaciones": model.DiasVacaciones = (int)nuevoValor; break;
				case "DiasAguinaldo": model.DiasAguinaldo = (int)nuevoValor; break;
				case "PrimaVacacional": model.PrimaVacacional = (decimal)nuevoValor; break;
			}

			decimal primaProporcional = model.PrimaVacacional / 100m;
			model.FactorIntegracion = Math.Round(
				1 + (((model.DiasVacaciones * primaProporcional) + model.DiasAguinaldo) / 365m), 6);
		}


		private async Task GuardarTodo()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");


				bool esNuevo = grupoModel.Id == 0;
				string url = esNuevo ? "api/GruposPrestaciones/Agregar" : $"api/GruposPrestaciones/Editar/{grupoModel.Id}";
				var metodo = HttpMethod.Post;

				var request = new HttpRequestMessage(metodo, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				request.Content = JsonContent.Create(grupoModel);
				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<object>>();

					if (resultado != null && resultado.IsSuccess)
					{

					
						await CargarGrupos();

						string mensajeExito = esNuevo ? "Registro creado con éxito" : "Registro actualizado correctamente";
						await JS.InvokeVoidAsync("notifier.success", mensajeExito);
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
		public class PrestacionLeyDetalle
		{
			public int Id { get; set; }
			public int Idgrupo_prestacion { get; set; }
			public int Anos { get; set; }
			public int DiasVacaciones { get; set; }
			public int DiasAguinaldo { get; set; }
			public decimal PrimaVacacional { get; set; }
			
			[JsonPropertyName("factor_Integracion")]
			public decimal FactorIntegracion { get; set; }
		}

		public class PrestacionLey
		{
			public int Id { get; set; }
			public string Descripcion { get; set; } = string.Empty;
			public string Activo { get; set; } = "S";
			public List<PrestacionLeyDetalle> Detalles { get; set; } = new();
		}




	}
}