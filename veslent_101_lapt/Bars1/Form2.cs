using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Data.SqlClient;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace Bars1
{    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();

            // X
            eks.MouseEnter += (s, a) =>
            { eks.ForeColor = Color.Red; };

            eks.MouseLeave += (s, a) =>
            { eks.ForeColor = Color.Indigo; };

            eks.MouseClick += (s, a) =>
            {
                this.Close();
                this.Dispose();
            };

            // minus
            menus.MouseEnter += (s, a) =>
            { menus.ForeColor = Color.LightSkyBlue; };

            menus.MouseLeave += (s, a) =>
            { menus.ForeColor = Color.Indigo; };

            menus.MouseClick += (s, a) =>
            { this.WindowState = FormWindowState.Minimized; };

        }

        // строка подключения
        const string cs = @"server=192.168.0.104;port=3306;username=root;password=12345;database=qwerty";

        // операции с таблицей sql
        private string init_sql = @"CREATE TABLE ves101 ( Id INTEGER NOT NULL PRIMARY KEY, h0 float NULL DEFAULT 0, h1 float NULL DEFAULT 0, h2 float NULL DEFAULT 0, h3 float NULL DEFAULT 0, h4 float NULL DEFAULT 0, h5 float NULL DEFAULT 0, h6 float NULL DEFAULT 0, h7 float NULL DEFAULT 0, h8 float NULL DEFAULT 0, h9 float NULL DEFAULT 0, h10 float NULL DEFAULT 0, h11 float NULL DEFAULT 0, old int NULL DEFAULT 0, hour int NULL DEFAULT 0)";
        MySqlConnection con = new MySqlConnection(cs);
        MySqlConnection coni = new MySqlConnection(cs);
        MySqlConnection cone = new MySqlConnection(cs);
        MySqlConnection cono = new MySqlConnection(cs);

        // moving form by header-panel
        bool drag = false;
        Point start_point = new Point(0, 0);
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            drag = true;
            start_point = new Point(e.X, e.Y);
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                Point p = PointToScreen(e.Location);
                this.Location = new Point(p.X - start_point.X, p.Y - start_point.Y);
            }
        }
        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        // массив заполняемый из БД
        float[] history = new float[14] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private async void Form2_Load(object sender, EventArgs e)
        {            
            Invoke((Action)(() =>
            {
                alarmo.Hide();
            }));

            // проверка наличия таблицы
            bool is_exist = false;
                string exist = $"SELECT * FROM qwerty.ves101;";
            con.Open();
            MySqlCommand cmd = new MySqlCommand(exist, con);
            try
            {
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        // если таблица есть, то true. иначе false
                        var wewe = dr.GetString(0);
                        if (wewe != null)
                            is_exist = true;
                    }
                }
            }
            catch { is_exist = false; }
            con.Close();

            // если таблицы нет, создаю
            if (!is_exist)
            {
                try
                {
                    con.Open();
                    MySqlCommand init = new MySqlCommand(init_sql, con);                    
                        init.ExecuteNonQuery();
                        is_exist = true;                    
                    con.Close();
                }
                catch { }
            }

            //// если таблица есть, но она пуста то забивка нулями
            bool nulls = !Rows_null();  // пусто, только nulls
            if (is_exist && nulls)
            {
                Zero();         // забивка нулями
            }

            // запуск потока чтения из БД (без предварительного включения)
            New_thread();

            // проверка связи с сервером
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Connect_to_06();
                    }
                    catch
                    {
                        Invoke((Action)(() =>
                        {
                            alarmo.Show();
                        }));
                    }
                }
            });
        }

        // проверка отсутствия занулений
        private bool Rows_null()
        {
            bool gusto = false;
            try
            {
                con.Open();                
                string nul = $"SELECT * FROM ves101 ORDER BY Id";
                MySqlCommand if_null = new MySqlCommand(nul, con);
                MySqlDataReader nu = if_null.ExecuteReader();
                gusto = nu.Read();      // true = что-то есть
                con.Close();
            }
            catch 
            {
                string nul = $"SELECT * FROM ves101 ORDER BY Id";
                MySqlCommand if_null = new MySqlCommand(nul, con);
                MySqlDataReader nu = if_null.ExecuteReader();
                gusto = nu.Read();      // true = что-то есть
            }
            return gusto;            
        }

        // создание модбас соединения с сервером. порт 502, регистры 10 и 12
        string vyvod_smena = "...";
        string vyvod_itog = "...";
        bool sc = false;    // sc - успешное соединение
        private async void Connect_to_06()
        {
            const string ip = "192.168.0.105";
            const int port = 502;

            var tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var data = new byte[] { 0, 0, 0, 0, 0, 6, 4, 3, 0, 10, 0, 4 };

            try
            {
                tcpSocket.Connect(tcpEndPoint);
                sc = true;
            }
            catch
            {
                if (sc)
                {
                    sc = false;
                    Invoke((Action)(() =>
                    {
                        alarmo.Show();
                    // чтение из БД
                        Archive(Subs(History(history)));

                    // вывод старого
                        Invoke((Action)(() =>
                        {
                            ranee.Text = Convert.ToString(erl);
                        }));
                    }));
                }
            }

            if (tcpSocket.Connected)
            {
                tcpSocket.Send(data);

                var answer = new StringBuilder();
                var size = 0;
                byte[] bag = new byte[256];

                await Task.Run(() =>
                {
                    do
                    {
                        try
                        {
                            size = tcpSocket.Receive(bag);
                            answer.Append(Encoding.UTF8.GetString(bag, 0, size));
                            if (bag[5] > 1)
                            {
                                Invoke((Action)(() =>
                                {
                                    alarmo.Hide();
                                }));
                            }
                        }
                        catch
                        {
                            Invoke((Action)(() =>
                            {
                                alarmo.Show();
                            }));
                        }
                    } while (tcpSocket.Available > 0);
                });

                tcpSocket.Shutdown(SocketShutdown.Both);
                tcpSocket.Close();

                byte[] bufic = new byte[4];  // buffer
                bufic[0] = bag[12];
                bufic[1] = bag[11];
                bufic[2] = bag[10];
                bufic[3] = bag[9];
                var full_int1 = BitConverter.ToUInt32(bufic, 0);
                float sm_fl = full_int1;
                vyvod_smena = Convert.ToString(full_int1);
                Invoke((Action)(() =>
                {
                    za_smenu.Text = vyvod_smena;
                }));
                smena = sm_fl;

                bufic[0] = bag[16];
                bufic[1] = bag[15];
                bufic[2] = bag[14];
                bufic[3] = bag[13];
                var full_int2 = BitConverter.ToUInt32(bufic, 0);
                float it_fl = full_int2;
                vyvod_itog = Convert.ToString(full_int2);
                Invoke((Action)(() =>
                {
                    za_vse.Text = vyvod_itog;
                }));

                can = true;     // разрешаю чтение из БД
            }            
        }

        // новый поток
        bool can = false;

        private void New_thread()
        {
            Thread nt = new Thread(() =>
            {
                Invoke((Action)(async() =>
                {
                    await Task.Run(() =>
                    {
                        while (true)
                        {
                            if (can)            // если разрешил, читаю
                            {
                                // если час сменился, то запись в БД
                                int cur_h = Current_hour();
                                bool smena_ch = Is_hour_gone(cur_h);
                                bool is_fs = Folstart(Current_hour(), smena, History(history));
                                bool is_tl = To_late(Current_hour(), smena, History(history));

                                if (!is_tl)                                         // нет опоздания
                                {
                                    if ((smena_ch && !is_fs)||(!smena_ch && is_fs)) // нет фольстарта
                                    {
                                        bool if_new = New_sm(Current_hour());
                                        if (!if_new)                                 // если смена не новая
                                            Shadow_put(Current_hour(), smena);      // то запись значений в БД
                                        else
                                            Updater();                              // иначе обновить таблицу
                                    }
                                }
                                // чтение из БД
                                Archive(Subs(History(history)));

                                // вывод старого
                                Invoke((Action)(() =>
                                {
                                    ranee.Text = Convert.ToString(erl); 
                                }));
                            }
                        }
                    });
                }));
            });
            nt.Start();
        }

        // метод фольстарта - новая смена в ПЛК наступает раньше часов ПК
        private bool Folstart(int hour, float current_val, float[] hist)
        {
            float max = hist.Max();
            if ((current_val < max) && ((hour == 7)||(hour == 19)))
                { return true; }
            else
                { return false; }
        }

        // метод опоздания - когда на ПК время пришло, а смена в ПЛК нет
        private bool To_late(int hour, float current_val, float[] hist)
        {
            float max = hist.Max();
            if ((current_val > max) && ((hour == 8) || (hour == 20)))
            { return true; }
            else
            { return false; }
        }

        // метод расчета часовых значений перед выводом
        private float[] Subs(float[] hist)
        {
            float[] secondry = new float[13] {0,0,0,0,0,0,0,0,0,0,0,0,0};
            secondry[0] = hist[0];
            float buffer = 0;
            for (int i = 1; i < 12; i++)
            {
                // если значение за текущий час меньше, чем за прежний, значит в БД данные за разные смены и ее очищаю
                if ((hist[i] != 0) && (hist[i - 1] != 0) && (hist[i] < hist[i-1]))
                {
                    con.Open();
                    for (int hi = 0; hi < 12; hi++)
                    {
                        string zero = $"UPDATE ves101 SET h{hi}=0 WHERE Id=0;";
                        MySqlCommand zr = new MySqlCommand(zero, con);
                         zr.ExecuteNonQuery(); 
                    }
                    con.Close();
                    return secondry;
                }

                if ((hist[i] != 0) && (hist[i-1] != 0))     // если есть текущее значение и предыдущее
                {
                    secondry[i] = hist[i] - hist[i-1];
                }
                else if ((hist[i] == 0) && (hist[i-1] != 0))    // если нет текущего, но но есть предыдущее
                {
                    buffer = hist[i-1];
                }    
                else if ((hist[i] == 0) && (hist[i-1] == 0))      // если все равны нулю
                { /* ничего */  }

                else if ((hist[i] != 0) && (hist[i - 1] == 0))    // если есть текущее есть, а прежних нет
                {
                    secondry[i] = hist[i] - buffer;
                }    
            }
            if (secondry.Sum() != 0)
            history_sum = secondry.Sum();
            return secondry;
        }

        // метод вывода истории на экран
        private void Archive(float[] shifted)
        {
            for (int i = 0; i < 11; i++)
            {
                string label_n = "lb" + i;
                foreach (Control ctrl in panel2.Controls)
                {
                    if (ctrl.Name == label_n)
                    {
                        Invoke((Action)(() =>
                        {
                            try
                            {
                                ctrl.Text = Convert.ToString(shifted[i+1]);
                            }
                            catch { }
                        }));
                    }
                }
            }

        }

        // float за смену
        float smena = 0;

        // получение времени
        private int Current_hour()
        {
            // игра со временем
            string time_now = Convert.ToString(DateTime.Now);    // текущее время ПК
            char[] tn_chars = time_now.ToCharArray();                // разбив значения на символы
            char[] chars_hourA = new char[1] { tn_chars[11] };
            char[] chars_hourB = new char[2] { tn_chars[11], tn_chars[12] };
            char[] chars_hourC = new char[1] { tn_chars[10] };
            char[] chars_hourD = new char[2] { tn_chars[10], tn_chars[11] };
            string current_hour = "13";
            char dot = Convert.ToChar(".");

            if ((tn_chars.Length == 18) && (tn_chars[2] == dot))
            { current_hour = new string(chars_hourA); }        // если час односимвольный
            else if ((tn_chars.Length == 19) && (tn_chars[2] == dot))
            { current_hour = new string(chars_hourB); }
            else if ((tn_chars.Length == 17) && (tn_chars[1] == dot))
            { current_hour = new string(chars_hourC); }
            else if ((tn_chars.Length == 18) && (tn_chars[1] == dot))
            { current_hour = new string(chars_hourD); }

            int hour = Convert.ToInt32(current_hour);           // результат текущего часа

            return hour;
        }

        // очистка в БД h0-h11 и вставка в "old" старого веса за смену при 8 или 20 часах
        private bool New_sm(int current_hour)
        {            
            if ((current_hour == 8) || (current_hour == 20))
            {
                return true;
            }
            else return false;
        }

        // метод для отладки
        private void Deb()
        {
            var new_old = (uint)(smena);
            try
            {
                con.Open();
                string change_smen = $"UPDATE ves101 SET old={new_old} WHERE Id=0;";
                MySqlCommand chsm = new MySqlCommand(change_smen, con);
                chsm.ExecuteNonQuery();
                con.Close();
            }
            catch
            {
                string change_smen = $"UPDATE ves101 SET old={new_old} WHERE Id=0;";
                MySqlCommand chsm = new MySqlCommand(change_smen, con);
                chsm.ExecuteNonQuery();
            }
        }

        // обновление новой смены
        private void Updater()
        {
            var new_old = (uint)(history_sum);

            try
            {
                con.Open();
                string change_smen = $"UPDATE ves101 SET old={new_old} WHERE Id=0;";
                MySqlCommand chsm = new MySqlCommand(change_smen, con);
                chsm.ExecuteNonQuery();
                con.Close();
            }
            catch
            {
                string change_smen = $"UPDATE ves101 SET old={new_old} WHERE Id=0;";
                MySqlCommand chsm = new MySqlCommand(change_smen, con);
                chsm.ExecuteNonQuery();
            }

            try
            {
                con.Open();
                for (int i = 0; i < 12; i++)
                {
                    string zero = $"UPDATE ves101 SET h{i}=0 WHERE Id=0;";
                    MySqlCommand zr = new MySqlCommand(zero, con);
                    zr.ExecuteNonQuery();
                }
                con.Close();
            }
            catch
            {
                for (int i = 0; i < 12; i++)
                {
                    string zero = $"UPDATE ves101 SET h{i}=0 WHERE Id=0;";
                    MySqlCommand zr = new MySqlCommand(zero, con);
                    zr.ExecuteNonQuery();
                }
            }
        }

        // смена часа. true когда час поменялся. старый лежит в бд
        int old_hour = 0;

        private bool Is_hour_gone(int current_hour)
        {
            Thread.Sleep(2000);
            bool bool_gone = false;
            string hour_old = $"SELECT * FROM ves101";

            coni.Open();
            MySqlCommand hour_elder = new MySqlCommand(hour_old, coni);
            MySqlDataReader ho = hour_elder.ExecuteReader();
                    {
                        while (ho.Read())
                        {
                            old_hour = Convert.ToInt32(ho[14]);
                        }
                        if (old_hour != current_hour)
                            bool_gone = true;
                    }
            coni.Close();
            return bool_gone;
        }

        // старый час сменяем на новый в БД
        private void Change_hour(int current_hour)
        {
            try
            {
                con.Open();
                string change_hour = $"UPDATE ves101 SET hour={current_hour} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, con);
                chhr.ExecuteNonQuery();
                con.Close();
            }
            catch 
            {
                string change_hour = $"UPDATE ves101 SET hour={current_hour} WHERE Id=0;";
                MySqlCommand chhr = new MySqlCommand(change_hour, con);
                chhr.ExecuteNonQuery();
            }
        }

        // буферизация
        private void Shadow_put(int curr_hour, float tonnaj_smena)
        {
            Change_hour(curr_hour);

            switch (curr_hour)
            {
                case 8:
                    { sql_update(0, tonnaj_smena); }
                    break;
                case 20:
                    { sql_update(0, tonnaj_smena); }
                    break;

                case 9:
                    { sql_update(1, tonnaj_smena); }
                    break;
                case 21:
                    { sql_update(1, tonnaj_smena); }
                    break;

                case 10:
                    { sql_update(2, tonnaj_smena); }
                    break;
                case 22:
                    { sql_update(2, tonnaj_smena); }
                    break;

                case 11:
                    { sql_update(3, tonnaj_smena); }
                    break;
                case 23:
                    { sql_update(3, tonnaj_smena); }
                    break;

                case 12:
                    { sql_update(4, tonnaj_smena); }
                    break;
                case 0:
                    { sql_update(4, tonnaj_smena); }
                    break;

                case 13:
                    { sql_update(5, tonnaj_smena); }
                    break;
                case 1:
                    { sql_update(5, tonnaj_smena); }
                    break;

                case 14:
                    { sql_update(6, tonnaj_smena); }
                    break;
                case 2:
                    { sql_update(6, tonnaj_smena); }
                    break;

                case 15:
                    { sql_update(7, tonnaj_smena); }
                    break;
                case 3:
                    { sql_update(7, tonnaj_smena); }
                    break;

                case 16:
                    { sql_update(8, tonnaj_smena); }
                    break;
                case 4:
                    { sql_update(8, tonnaj_smena); }
                    break;

                case 17:
                    { sql_update(9, tonnaj_smena); }
                    break;
                case 5:
                    { sql_update(9, tonnaj_smena); }
                    break;

                case 18:
                    { sql_update(10, tonnaj_smena); }
                    break;
                case 6:
                    { sql_update(10, tonnaj_smena); }
                    break;

                case 19:
                    { sql_update(11, tonnaj_smena); }
                    break;
                case 7:
                    { sql_update(11, tonnaj_smena); }
                    break;
            };
        }

        // команда в БД на изменение значения
        private void sql_update(int idh, float flowh)
        {
            int full_int3 = Convert.ToInt32(flowh);
            try
            {
                con.Open();
                //string update_table_value = $"UPDATE [Table] SET h{idh}={flowh} WHERE Id=0;";
                string update_table_value = $"UPDATE ves101 SET h{idh}={full_int3} WHERE Id=0;";
                MySqlCommand upd = new MySqlCommand(update_table_value, con);
                upd.ExecuteNonQuery();
                con.Close();
            }
            catch
            {
                //string update_table_value = $"UPDATE [Table] SET h{idh}={flowh} WHERE Id=0;";
                string update_table_value = $"UPDATE ves101 SET h{idh}={full_int3} WHERE Id=0;";
                MySqlCommand upd = new MySqlCommand(update_table_value, con);
                upd.ExecuteNonQuery();
            }
        }

        // команда в БД на на инициализацию нулевыми значениями
        private void Zero()
        {
            try
            {
                con.Open();
                string insertation_0 = $"INSERT INTO ves101 (Id, h0, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11, old, hour) VALUES ('{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}', {Current_hour()})";
                MySqlCommand zero = new MySqlCommand(insertation_0, con);
                zero.ExecuteNonQuery();
                con.Close();
            }
            catch 
            {
                string insertation_0 = $"INSERT INTO ves101 (Id, h0, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11, old, hour) VALUES ('{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}','{0}', {Current_hour()})";
                MySqlCommand zero = new MySqlCommand(insertation_0, con);
                zero.ExecuteNonQuery();
            }
        }

        // чтение истории значений
        float erl = 0;
        float history_sum = 0;
        private float[] History(float[] hist)
        {
            string read = $"SELECT * FROM qwerty.ves101";

            try
            {
                cone.Open();

                Thread.Sleep(50);
                MySqlCommand reading = new MySqlCommand(read, cone);
                Thread.Sleep(50);
                MySqlDataReader rh = reading.ExecuteReader();
                while (rh.Read())
                {
                    for (int i = 1; i < 13; i++)
                    {
                        // читаю значения веса
                        hist[i - 1] = Convert.ToSingle(rh[i]);
                    }

                    // читаю значение за старую смену
                    erl = Convert.ToSingle(rh[13]);

                }

                cone.Close();
            }
            catch { }
            return hist;
        }

        private void rescan2_Click(object sender, EventArgs e)
        {
            string zer = $"UPDATE ves101 SET h0=0, h1=0, h2=0, h3=0, h4=0, h5=0, h6=0, h7=0, h8=0, h9=0, h10=0, h11=0, old=0  WHERE Id=0";

            cono.Open();
            MySqlCommand zer0 = new MySqlCommand(zer, cono);
            zer0.ExecuteNonQuery();
            cono.Close();
        }
    }
}
       