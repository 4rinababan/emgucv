Imports Emgu.CV.Structure
Imports Emgu.CV
Public Class Class1
    Private _eiginImages As Image(Of Gray, Single)()
    Private _avgImage As Image(Of Gray, Single)
    Private _eigenValues As Matrix(Of Single)()
    Private _labels As String()
    Private _eigenDistanceThreeshold As Double
    Private _eigenDistance1 As Single
    Private hasil As String

    Public Property EigenImages As Image(Of Gray, Single)()
        Get
            Return _eiginImages

        End Get
        Set(ByVal value As Image(Of Gray, Single)())
            _eiginImages = value
        End Set
    End Property

    Public Property Labels() As String()
        Get
            Return _labels
        End Get
        Set(ByVal value As String())
            _labels = value
        End Set
    End Property

    Public Property EigenDistanceThreeshold As Double
        Set(ByVal value As Double)
            _eigenDistanceThreeshold = value
        End Set
        Get
            Return _eigenDistanceThreeshold
        End Get
    End Property

    Public Property AverangeImages As Image(Of Gray, Single)
        Get
            Return _avgImage
        End Get
        Set(ByVal value As Image(Of Gray, Single))
            _avgImage = value
        End Set
    End Property

    Public Property EigenValues() As Matrix(Of Single)()
        Get
            Return _eigenValues
        End Get
        Set(ByVal value As Matrix(Of Single)())
            _eigenValues = value
        End Set
    End Property

    Private Sub New()

    End Sub

    Public Sub New(images As Image(Of Gray, [Byte])(), ByRef termCrit As MCvTermCriteria)
        Me.New(images, GenerateLabels(images.Length), termCrit)
    End Sub

    Private Shared Function GenerateLabels(ByVal size As Integer) As [String]()
        Dim labels As [String]() = New String(size - 1) {}
        For i As Integer = 0 To size - 1
            labels(i) = i.ToString()
        Next
        Return labels
    End Function

    Public Sub New(images As Image(Of Gray, [Byte])(), ByVal labels As [String](), ByRef termCrit As MCvTermCriteria)
        Me.New(images, labels, 0, termCrit)
    End Sub

    Public Sub New(ByVal images As Image(Of Gray, [Byte])(), ByVal labels As [String](), ByVal eigenDistanceThreshold As Double, ByRef termCrit As MCvTermCriteria)
        Debug.Assert(images.Length = labels.Length, "Jumlah gambar harus sama dengan jumlah label")
        Debug.Assert(eigenDistanceThreshold >= 0.0, "Eigen Threesold harus selalu >= 0.0")

        CalcEigenObjects(images, termCrit, _eiginImages, _avgImage)

        _eigenValues = Array.ConvertAll(Of Image(Of Gray, [Byte]), Matrix(Of Single)) _
            (images, Function(img As Image(Of Gray, [Byte])) New Matrix(Of Single)(EigenDecomposite(img, _eiginImages, _avgImage)))
        _labels = labels
        _eigenDistanceThreeshold = eigenDistanceThreshold
    End Sub

    Public Shared Sub CalcEigenObjects(ByVal trainingImages As Image(Of Gray, [Byte])(),
            ByRef termCrit As MCvTermCriteria, ByRef eigenImages As Image(Of Gray, [Single])(),
                                       ByRef avg As Image(Of Gray, [Single]))
        Dim width As Integer = trainingImages(0).Width
        Dim height As Integer = trainingImages(0).Height

        Dim inObjs As IntPtr() = Array.ConvertAll(Of Image(Of Gray, [Byte]), IntPtr)(trainingImages,
Function(img As Image(Of Gray, [Byte])) img.Ptr)

        If termCrit.max_iter <= 0 OrElse termCrit.max_iter > trainingImages.Length Then
            termCrit.max_iter = trainingImages.Length
        End If

        Dim maxEigenObjs As Integer = termCrit.max_iter

        eigenImages = New Image(Of Gray, Single)(maxEigenObjs - 1) {}
        For i As Integer = 0 To eigenImages.Length - 1
            eigenImages(i) = New Image(Of Gray, Single)(width, height)
        Next

        Dim eigObjs As IntPtr() = Array.ConvertAll(Of Image(Of Gray, [Single]), IntPtr) _
            (eigenImages, Function(img As Image(Of Gray, [Single])) img.Ptr)

        avg = New Image(Of Gray, [Single])(width, height)

        CvInvoke.cvCalcEigenObjects(inObjs, termCrit, eigObjs, Nothing, avg.Ptr)
    End Sub

    Public Shared Function EigenDecomposite(ByVal src As Image(Of Gray, [Byte]),
        ByVal eigenImages As Image(Of Gray, [Single])(),
        ByVal avg As Image(Of Gray, [Single])) As Single()
        Return CvInvoke.cvEigenDecomposite(src.Ptr, Array.ConvertAll(Of Image(Of Gray, [Single]), IntPtr) _
            (eigenImages, Function(img As Image(Of Gray, [Single])) img.Ptr), avg.Ptr)
    End Function

    Public Function EigenProjection(ByVal eigenValue As Single()) As Image(Of Gray, [Byte])
        Dim res As Image(Of Gray, [Byte]) = New Image(Of Gray, Byte)(_avgImage.Width, _avgImage.Height)

        CvInvoke.cvEigenProjection(Array.ConvertAll(Of Image(Of Gray, [Single]), IntPtr) _
            (_eiginImages, Function(img As Image(Of Gray, [Single])) img.Ptr), eigenValue, _avgImage.Ptr, res.Ptr)
        Return res
    End Function

    Public Function GetEigenDistances(ByVal image As Image(Of Gray, [Byte])) As Single()
        Using eigenvalue As New Matrix(Of Single)(EigenDecomposite(image, _eiginImages, _avgImage))
            Return Array.ConvertAll(Of Matrix(Of Single), Single) _
                (_eigenValues, Function(eigenValueI As Matrix(Of Single)) _
                                   CSng(CvInvoke.cvNorm(eigenvalue.Ptr, eigenValueI.Ptr, Emgu.CV.CvEnum.NORM_TYPE.CV_L2, IntPtr.Zero)))


        End Using
    End Function

    Public ReadOnly Property Get_Eigen_Distance() As Single
        Get
            Return _eigenDistance1
        End Get
    End Property

    Public Sub FindMostSimilarObject(ByVal image As Image(Of Gray, [Byte]), ByRef index As Integer,
                                     ByRef eigenDistance As Single, ByRef label As [String])

        Dim dist As Single() = GetEigenDistances(image)
        index = 0
        eigenDistance = dist(0)
        For i As Integer = 1 To dist.Length - 1
            If dist(i) < eigenDistance Then
                index = i
                eigenDistance = dist(i)
            End If
        Next
        label = Labels(index)

    End Sub

    Public Function Recognize(ByVal image As Image(Of Gray, [Byte]), ByVal status As Integer) As [String]
        Dim index As Integer
        Dim eigenDistance As Single
        Dim label As [String]

        FindMostSimilarObject(image, index, eigenDistance, label)
        _eigenDistance1 = eigenDistance

        If status = 1 Then
            hasil = If((_eigenDistanceThreeshold <= 0 OrElse eigenDistance > _eigenDistanceThreeshold _
                AndAlso eigenDistance < 5000), _labels(index), "Tidak Dikenal")
        ElseIf status = 0 Then
            hasil = If((_eigenDistanceThreeshold <= 0 OrElse eigenDistance > _eigenDistanceThreeshold _
                AndAlso eigenDistance < 4700), _labels(index), "Tidak Dikenal")

        End If
        Return (hasil)

    End Function

End Class
