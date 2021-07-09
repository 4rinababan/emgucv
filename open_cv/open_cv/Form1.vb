Imports System.IO
Imports System.Xml
Imports System.Threading

Imports Emgu.CV
Imports Emgu.Util
Imports Emgu.CV.Structure
Imports Emgu.CV.CvEnum
Imports DirectShowLib

Public Class Form1
    Private currentFrame As Image(Of Bgr, [Byte])
    Private grabber As Capture
    Private face As HaarCascade

    Private font As New MCvFont(CvEnum.FONT.CV_FONT_HERSHEY_COMPLEX, 0.3, 0.3)
    Private result As Image(Of Gray, Byte) = Nothing
    Private gray As Image(Of Gray, Byte) = Nothing
    Private trainingImages As New List(Of Image(Of Gray, Byte))()
    Private labels As New List(Of String)()
    Private NamePersons As New List(Of String)()
    Private Contrain As Integer, Numlabels As Integer, g As Integer
    Private name As String, names As String = Nothing
    Private Wtd As Integer
    Private DefaultThreshold As Double = 3000
    Private Names_List As New List(Of String)()
    Private Names_List_ID As New List(Of Integer)()

    Dim Pengenalanwajah As Class1

    Public Sub New()


        ' This call is required by the designer.
        InitializeComponent()
        bacakamera()
        face = New HaarCascade("haarcasade_fontalface_alt.xml")
        loadtrainingdata()
        ComboBox1.Text = Convert.ToString(ComboBox1.Items(0))

        TextBox1.Text = Convert.ToString(GetThreshold)

        ' Add any initialization after the InitializeComponent() call.

    End Sub
    Public Sub loadtrainingdata()
        If File.Exists(Application.StartupPath + "/db_img/TrainedLabels.xml") Then
            Try
                Names_List.Clear()
                Names_List_ID.Clear()
                trainingImages.Clear()
                Numlabels = 0
                labels.Clear()

                Dim filestream As FileStream = File.OpenRead(Application.StartupPath + "/db_img/TrainedLabels.xml")
                Dim filelength As Long = filestream.Length
                Dim xmlBytes As Byte() = New Byte(filelength - 1) {}
                filestream.Read(xmlBytes, 0, CInt(filelength))
                filestream.Close()

                Dim xmlStream As New MemoryStream(xmlBytes)

                Using xmlreader As XmlReader = XmlTextReader.Create(xmlStream)
                    While xmlreader.Read()
                        If xmlreader.IsStartElement() Then
                            Select Case xmlreader.Name
                                Case "NAMA"
                                    If xmlreader.Read() Then
                                        Names_List_ID.Add(Names_List.Count)
                                        '0, 1, 2, 3.....
                                        Names_List.Add(xmlreader.Value.Trim())
                                        labels.Add(xmlreader.Value.Trim())
                                        Numlabels += 1
                                    End If
                                    Exit Select
                                Case "NamaFile"
                                    If xmlreader.Read() Then
                                        trainingImages.Add(New Image(Of Gray, Byte)(Application.StartupPath + "\db_img" + xmlreader.Value.Trim()))

                                    End If
                                    Exit Select
                            End Select
                        End If
                    End While
                End Using
                Contrain = Numlabels
            Catch ex As Exception
                MessageBox.Show("Database Masih Kosong", "Training Face", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End Try
        End If
    End Sub
    Public ReadOnly Property GetThreshold() As Double
        Get
            Return DefaultThreshold
        End Get
    End Property

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If Button1.Text = "Start" Then
            Try
                grabber = New Capture(ComboBox1.SelectedIndex)
                SetThreshold(Convert.ToDouble(TextBox1.Text))
                grabber.QueryFrame()

                AddHandler Application.Idle, AddressOf FrameGrabber

                Button1.Text = "Stop"
                ComboBox1.Enabled = False
                TextBox1.Enabled = False
                Dim termCrit As New MCvTermCriteria(Contrain, 0.001)
                Pengenalanwajah = New Class1(trainingImages.ToArray(), labels.ToArray(), GetThreshold, termCrit)
            Catch ex As Exception
                MessageBox.Show("Gagal Mendapatkan Device Camera", "Failed")
            End Try

        ElseIf Button1.Text = "Stop" Then
            RemoveHandler Application.Idle, AddressOf FrameGrabber

            If grabber IsNot Nothing Then
                grabber.Dispose()
                PictureBox1.Image = Nothing
                Button1.Text = "Start"
                ComboBox1.Enabled = True
                TextBox1.Enabled = True
            End If
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim FrmTraining As New Form2
        FrmTraining.Show()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Public Function SetThreshold(ByVal nilai As Double)
        DefaultThreshold = nilai
        Return DefaultThreshold
    End Function

    Private Sub FrameGrabber(ByVal sender As Object, ByVal e As EventArgs)
        NamePersons.Add("")
        currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC)

        gray = currentFrame.Convert(Of Gray, [Byte])()
        Dim faceDetected As MCvAvgComp()() = gray.DetectHaarCascade(face, 1.2, 8, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, New Size(50, 50))

        For Each f As MCvAvgComp In faceDetected(0)
            result = currentFrame.Copy(f.rect).Convert(Of Gray, Byte)().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC)

            result._EqualizeHist()
            currentFrame.Draw(f.rect, New Bgr(Color.BlueViolet), 2)
            If trainingImages.ToArray().Length <> 0 Then
                name = Pengenalanwajah.Recognize(result, 0)
                Dim match_value As Integer = CInt(Pengenalanwajah.Get_Eigen_Distance)
                Dim NamaDetected As [String] = "Nama : " + name.ToUpper()
                Dim EigDist As [String] = "Distance : " + Convert.ToString(match_value)

                currentFrame.Draw(NamaDetected, font, New Point(f.rect.X - 2, f.rect.Y - 13), New Bgr(Color.BurlyWood))
                currentFrame.Draw(EigDist, font, New Point(f.rect.X - 2, f.rect.Y - 4), New Bgr(Color.BurlyWood))

            End If

            If Contrain < 1 Then
                currentFrame.Draw("NAMA : Tidak Dikenal", font, New Point(f.rect.X - 2, f.rect.Y - 4), New Bgr(Color.BurlyWood))
            End If
        Next
        PictureBox1.Image = currentFrame.ToBitmap
    End Sub

    Private Sub bacakamera()
        Dim drivCam() As DsDevice
        drivCam = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice)
        For Each d As DsDevice In drivCam
            ComboBox1.Items.Add(d.Name)
        Next
    End Sub

End Class
