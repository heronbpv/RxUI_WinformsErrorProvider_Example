using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace ErrorProviderExample
{
    public class Form1ViewModel : ReactiveValidationObject 
    {
        public Form1ViewModel()
        {
            this.ValidationRule(
                vm => vm.User,
                u => !string.IsNullOrWhiteSpace(u),
                "Field User is mandatory."
            );
            
            //The example on the repository shows multiple calls to ValidationRule in order to apply various validations
            //to the same property. This doesn't seem to work in my case, as the second rule  
            this.ValidationRule(
                vm => vm.Password,
                pass => !string.IsNullOrWhiteSpace(pass),
                "Field Password is mandatory."
            );

            this.ValidationRule(
                vm => vm.Password,
                pass => pass?.Length > 2,
                "Password must contain at least three characters."
            );

            //Since this depends on the state of the IsValid property, it must come later;
            //Is there a way to monitor the validity of the context in real time, as in via observable? Just so the 
            //order doesn't matter in this case.
            this.Login =
                ReactiveCommand
                    .CreateFromObservable(
                        execute:
                        () => this.Logon.Handle(Unit.Default),
                        canExecute:
                        this.WhenAnyValue(
                                property1: vm => vm.User,
                                property2: vm => vm.Password,
                                selector:
                                (u, p) =>
                                    !string.IsNullOrWhiteSpace(u)
                                    && !string.IsNullOrWhiteSpace(p)
                                    && this.ValidationContext.IsValid
                            )
                    );
        }
        
        private string user;

        public string User
        {
            get => user;
            set => this.RaiseAndSetIfChanged(ref user, value);
        }

        private string password;

        public string Password
        {
            get => password;
            set => this.RaiseAndSetIfChanged(ref password, value);
        }

        public ReactiveCommand<Unit, Unit> Login { get; }

        public Interaction<Unit, Unit> Logon { get; } = new Interaction<Unit, Unit>();
    }
}