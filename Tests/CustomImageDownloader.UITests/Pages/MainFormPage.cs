using FlaUI.Core.AutomationElements;

namespace CustomImageDownloader.UITests.Pages;

public class MainFormPage
{
    private readonly Window _window;

    public MainFormPage(Window window) => 
        _window = window;

    public TextBox TxtUrls => Find("txtUrls").AsTextBox();
    public TextBox TxtCarpeta => Find("txtCarpeta").AsTextBox();
    public TextBox TxtNombreBase => Find("txtNombreBase").AsTextBox();
    public Button BtnDescargar => Find("btnDescargar").AsButton();
    public Button BtnCancelar => Find("btnCancelar").AsButton();
    public Button BtnPausar => Find("btnPausar").AsButton();
    public Button BtnSeleccionarCarpeta => Find("btnSeleccionarCarpeta").AsButton();
    public Button BtnLimpiar => Find("btnLimpiar").AsButton();
    public Label LblEstado => Find("lblEstado").AsLabel();
    public ProgressBar PbProgreso => Find("pbProgreso").AsProgressBar();

    public AutomationElement NumConcurrencia => Find("numConcurrencia");

    private AutomationElement Find(string name) =>
        _window.FindFirstDescendant(cf => cf.ByAutomationId(name))
        ?? throw new InvalidOperationException($"Control '{name}' not found in the UI tree.");
}
