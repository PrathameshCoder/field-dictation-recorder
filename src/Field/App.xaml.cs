using System.Windows;
namespace Field;
public partial class App : System.Windows.Application
{
 System.Threading.Mutex? instance;
 System.Threading.EventWaitHandle? reopen;
 System.Threading.RegisteredWaitHandle? wait;
 bool ownsInstance;
 public static bool IsEnding { get; private set; }
 protected override void OnStartup(StartupEventArgs e)
 {
  // Test hosts construct App too; only the shipped executable owns the instance lock.
  if (System.Reflection.Assembly.GetEntryAssembly() == typeof(App).Assembly)
  {
   var name = @"Local\Field-" + Environment.UserName;
   instance = new System.Threading.Mutex(true, name, out ownsInstance);
   reopen = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, name + "-show");
   if (!ownsInstance) { reopen.Set(); Shutdown(); return; }
   wait = System.Threading.ThreadPool.RegisterWaitForSingleObject(reopen, (_, _) => Dispatcher.BeginInvoke(() => (MainWindow as Field.MainWindow)?.ShowMain()), null, -1, false);
  }
  base.OnStartup(e);
 }
 protected override void OnSessionEnding(SessionEndingCancelEventArgs e) { IsEnding = true; base.OnSessionEnding(e); }
 protected override void OnExit(ExitEventArgs e) { wait?.Unregister(null); reopen?.Dispose(); if (ownsInstance) instance?.ReleaseMutex(); instance?.Dispose(); base.OnExit(e); }
}
