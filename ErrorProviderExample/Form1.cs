using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Validation.Components.Abstractions;

namespace ErrorProviderExample
{
    public partial class Form1 : Form, IViewFor<Form1ViewModel>
    {
        public Form1(Form1ViewModel viewModel)
        {
            this.ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            InitializeComponent();
            
            this.WhenActivated(
                disposables =>
                {
                    this
                        .Bind(this.ViewModel, vm => vm.User, v => v.tbUser.Text)
                        .DisposeWith(disposables);
                    
                    this
                        .Bind(this.ViewModel, vm => vm.Password, v => v.tbPassword.Text)
                        .DisposeWith(disposables);
                    
                    this
                        .BindCommand(this.ViewModel, vm => vm.Login, v => v.btLogin)
                        .DisposeWith(disposables);
                    
                    this
                        .ViewModel?
                        .Logon
                        .RegisterHandler(
                            context =>
                            {
                                MessageBox.Show("Logged In!");

                                this.tbUser.Text = "";
                                this.tbPassword.Text = "";
                                
                                context.SetOutput(Unit.Default);
                            })
                        .DisposeWith(disposables);
                    
                    //My best take at getting the list of validation errors out of the ValidationContext, so far.
                    //It seems messy, but I took the idea directly from ReactiveValidationObject's ctor though...
                    var propVals = 
                            this
                                .ViewModel?
                                .ValidationContext
                                .Validations
                                .ToObservableChangeSet()
                                .ToCollection()
                                .Select(
                                    comps =>
                                        comps
                                            .Select(
                                                comp => 
                                                    comp
                                                        .ValidationStatusChange
                                                        .Select(_ => comp))
                                            .Merge() //Merge instead of Switch, as in the reference ctor.  
                                            .StartWith(this.ViewModel.ValidationContext)
                                )
                                .Merge()
                                .Where(comp => comp is IPropertyValidationComponent)
                                .Cast<IPropertyValidationComponent>();
                    
                    //The idea is to use the list of errors to call the error provider set error function when appropriate.
                    //However, the fields are starting already marked as error, and in the case of password, only the last
                    //one is being shown. How can I prevent the observable to start in error, and how does one get all errors
                    //associated with a single property?
                    propVals?
                        .Where(x => x.ContainsPropertyName(nameof(this.ViewModel.User)))
                        .Do(
                            x => 
                                this.errorProvider.SetError(this.tbUser, x.Text?.ToSingleLine("\n")))
                        .Subscribe()
                        .DisposeWith(disposables);
                    
                    propVals?
                        .Where(x => x.ContainsPropertyName(nameof(this.ViewModel.Password)))
                        .Do(
                            x =>
                                //When multiple errors happen, isn't this supposed to return all error messages?
                                this.errorProvider.SetError(this.tbPassword, x.Text?.ToSingleLine("\n")))
                        .Subscribe()
                        .DisposeWith(disposables);
                }
            );
        }

        #region Implementation of IViewFor

        /// <inheritdoc />
        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (Form1ViewModel)value;
        }

        #endregion

        #region Implementation of IViewFor<Form1ViewModel>

        /// <inheritdoc />
        public Form1ViewModel? ViewModel { get; set; }

        #endregion
    }
}
