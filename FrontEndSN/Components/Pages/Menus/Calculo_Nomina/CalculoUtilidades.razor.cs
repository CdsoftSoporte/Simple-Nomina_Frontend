using Microsoft.AspNetCore.Components.Forms;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class CalculoUtilidades
	{

		private bool cargando = false;
		private bool DescontarFaltas = true;

		private decimal CantidadARepartir = 0M;
		
		private int IdPatronSeleccionado = 0;
		private int IdNominaSeleccionada = 0;
		private int IdPeriodoSeleccionado = 0;



		private bool archivoCargado = false;
		private string nombreArchivo = "";
		private byte[] excelEnMemoria;

		private char LimiteSeleccionado = '1';
		private List<dynamic> OpcionesLimite = new()
		{
			new { Valor = '1', Texto = "Calcular prom. de los últimos 3 años" },
			new { Valor = '2', Texto = "3 Meses de sueldo"  },
			new { Valor = '3', Texto = "Promedio de los últimos 3 años (Excel)"  },
			new { Valor = '4', Texto = "No topar ingreso por utilidades"  },

		};

		private char TopeMesesSeleccionado = '1';
		private List<dynamic> OpcionesTopeMeses = new()
		{
			new { Valor = '1', Texto = "Salario Diario" },
			new { Valor = '2', Texto = "Salario Diario Integrado"  }
		};


		protected override async Task OnInitializedAsync()
		{
			
			await Task.WhenAll(
					CargarPatronesParaCombo()
						);
			StateHasChanged();
		}




		protected async Task OnLimiteChanged(char nuevaSeleccion)
		{
			LimiteSeleccionado = nuevaSeleccion;
			nombreArchivo = "";
			excelEnMemoria = [];
			archivoCargado = false;
			StateHasChanged();
		}

		protected async Task OnTopeMesesChanged(char nuevaSeleccion)
		{
			TopeMesesSeleccionado = nuevaSeleccion;
		}

		protected async Task OnPatronChanged(int nuevoPatron)
		{
			IdPatronSeleccionado = nuevoPatron;
			await CargarNominasParaCombo(IdPatronSeleccionado);
		}
		protected async Task OnNominaChanged(int nuevaNomina)
		{
			IdNominaSeleccionada = nuevaNomina;
			await CargarPeriodosParaCombo(IdNominaSeleccionada);
		}

		protected async Task OnPeriodoChanged(int nuevoPeriodo)
		{
			IdPeriodoSeleccionado = nuevoPeriodo;

		}



		private async Task GenerarImportes()
		{

		}




		private async Task OnArchivoSeleccionado(InputFileChangeEventArgs e)
		{
			var archivoExcel = e.File;
			nombreArchivo = archivoExcel.Name;

			// Limitar a 5MB por seguridad (ajusta según necesites)
			long maxAllowedSize = 5120000;

			using var stream = archivoExcel.OpenReadStream(maxAllowedSize);
			using var ms = new MemoryStream();
			await stream.CopyToAsync(ms);

			// Guardamos el excel en memoria RAM
			excelEnMemoria = ms.ToArray();
			archivoCargado = true;
		}

		private async Task DescargarPlantilla()
		{
			// Aquí va tu lógica para solicitar la plantilla vacía al backend 
			// y descargarla usando JSInterop
		}



	}
}
