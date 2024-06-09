﻿Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net
Imports AForge.Imaging.Filters


Public Class edit

    Private originalImage As Bitmap
    Private editedImage As Bitmap = originalImage
    Private croppedImage As Bitmap ' Use croppedImage instead of editedImage for cropping
    Private isCropping As Boolean = False
    Private cropStartPoint As Point
    Private cropEndPoint As Point

    Private db As New sqlcontrol("Server=NIRAJ;Database=imgdatabase;Integrated Security=True")


    Private Sub editpb_DoubleClick(sender As Object, e As EventArgs) Handles PictureBox1.DoubleClick
        Using openFileDialog As New OpenFileDialog()
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp"
            If openFileDialog.ShowDialog() = DialogResult.OK Then
                originalImage = CType(Image.FromFile(openFileDialog.FileName), Bitmap)
                editedImage = ConvertTo24bppRgb(originalImage)
                PictureBox1.Image = editedImage
            End If
        End Using
    End Sub

    Private Function ConvertTo24bppRgb(img As Bitmap) As Bitmap
        Dim bmp As New Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.DrawImage(img, New Rectangle(0, 0, bmp.Width, bmp.Height))
        End Using
        Return bmp
    End Function

    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs) Handles filterbtn.Click
        panelfilters.Visible = True
        panelcrop.Visible = False
        paneladjust.Visible = False
    End Sub

    Private Sub btncropopen_Click(sender As Object, e As EventArgs) Handles btncropopen.Click
        panelfilters.Visible = False
        panelcrop.Visible = True
        paneladjust.Visible = False
    End Sub

    Private Sub btnadjust_Click(sender As Object, e As EventArgs) Handles btnadjust.Click
        panelfilters.Visible = False
        panelcrop.Visible = False
        paneladjust.Visible = True
    End Sub

    Private Sub btnfiltergray_Click(sender As Object, e As EventArgs) Handles btnfiltergray.Click
        If editedImage IsNot Nothing Then
            Try
                editedImage = ConvertTo24bppRgb(editedImage)
                Dim grayscaleFilter As New Grayscale(0.2125, 0.7154, 0.0721)
                editedImage = grayscaleFilter.Apply(editedImage)
                PictureBox1.Image = editedImage
            Catch ex As Exception
                MessageBox.Show("Error applying grayscale filter: " & ex.Message)
            End Try
        End If
    End Sub



    Private Sub btnrotateright_Click(sender As Object, e As EventArgs) Handles btnrotateright.Click
        If editedImage IsNot Nothing Then
            editedImage.RotateFlip(RotateFlipType.Rotate90FlipNone)
            PictureBox1.Image = editedImage
        End If
    End Sub



    Private Sub btnsave_Click(sender As Object, e As EventArgs) Handles btnsave.Click
        If editedImage IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(txtcaption.Text) AndAlso Not String.IsNullOrWhiteSpace(userid) Then
            Try



                Using ms As New MemoryStream()
                    Dim codecInfo As ImageCodecInfo = GetEncoder(ImageFormat.Jpeg)
                    Dim encoder As System.Drawing.Imaging.Encoder = System.Drawing.Imaging.Encoder.Quality
                    Dim encoderParams As New EncoderParameters(1)
                    encoderParams.Param(0) = New EncoderParameter(encoder, 100L)

                    editedImage.Save(ms, codecInfo, encoderParams)
                    Dim imageData As Byte() = ms.ToArray()

                    db.AddParam("@UserID", Convert.ToInt32(userid))
                    db.AddParam("@ImageData", imageData)
                    db.AddParam("@Caption", txtcaption.Text)
                    db.AddParam("@UploadDate", DateTime.Now)

                    db.ExecQuery("INSERT INTO UserUploads (UserID, ImageData, Caption, UploadDate) VALUES (@UserID, @ImageData, @Caption, @UploadDate)")

                    If db.HasException(True) Then Exit Sub

                    MessageBox.Show("Image uploaded successfully!")
                End Using
            Catch ex As Exception
                MessageBox.Show("Error saving image: " & ex.Message)
            End Try
        Else
            MessageBox.Show("Please select an image, enter a caption, and provide a valid user ID.")
        End If
    End Sub

    Private Function GetEncoder(format As ImageFormat) As ImageCodecInfo
        Dim codecs As ImageCodecInfo() = ImageCodecInfo.GetImageDecoders()
        For Each codec As ImageCodecInfo In codecs
            If codec.FormatID = format.Guid Then
                Return codec
            End If
        Next
        Return Nothing
    End Function

    Private Sub btnfiltersepia_Click(sender As Object, e As EventArgs) Handles btnfiltersepia.Click
        If editedImage IsNot Nothing Then
            Try
                editedImage = ConvertTo24bppRgb(editedImage)
                Dim sepiaFilter As New Sepia()
                editedImage = sepiaFilter.Apply(editedImage)
                PictureBox1.Image = editedImage
            Catch ex As Exception
                MessageBox.Show("Error applying sepia filter: " & ex.Message)
            End Try
        End If
    End Sub
    Private Sub PictureBox1_MouseDown(sender As Object, e As MouseEventArgs) Handles PictureBox1.MouseDown
        If originalImage IsNot Nothing AndAlso e.Button = MouseButtons.Left Then
            isCropping = True
            cropStartPoint = e.Location
        End If
    End Sub


    Private Sub PictureBox1_MouseMove(sender As Object, e As MouseEventArgs) Handles PictureBox1.MouseMove
        If isCropping Then
            cropEndPoint = e.Location
            PictureBox1.Invalidate() ' Invalidate to show selection rectangle
        End If
    End Sub

    Private Sub PictureBox1_MouseUp(sender As Object, e As MouseEventArgs) Handles PictureBox1.MouseUp
        If isCropping Then
            isCropping = False
            CropImage(cropStartPoint, cropEndPoint)
            ' Invalidate to refresh the picture box
        End If
    End Sub

    Private Sub PictureBox1_Paint(sender As Object, e As PaintEventArgs) Handles PictureBox1.Paint
        If isCropping Then
            Dim cropRect As New Rectangle(
                Math.Min(cropStartPoint.X, cropEndPoint.X),
                Math.Min(cropStartPoint.Y, cropEndPoint.Y),
                Math.Abs(cropStartPoint.X - cropEndPoint.X),
                Math.Abs(cropStartPoint.Y - cropEndPoint.Y)
            )
            e.Graphics.DrawRectangle(Pens.Red, cropRect) ' Draw selection rectangle
        End If
    End Sub



    Private Sub CropImage(startPoint As Point, endPoint As Point)
        ' Adjust coordinates if the image size is smaller than the picture box size
        Dim scaleX As Double = editedImage.Width / PictureBox1.Width
        Dim scaleY As Double = editedImage.Height / PictureBox1.Height

        startPoint.X = CInt(startPoint.X * scaleX)
        startPoint.Y = CInt(startPoint.Y * scaleY)
        endPoint.X = CInt(endPoint.X * scaleX)
        endPoint.Y = CInt(endPoint.Y * scaleY)

        Dim cropRect As New Rectangle(
        Math.Min(startPoint.X, endPoint.X),
        Math.Min(startPoint.Y, endPoint.Y),
        Math.Abs(startPoint.X - endPoint.X),
        Math.Abs(startPoint.Y - endPoint.Y)
    )

        If cropRect.Width > 0 AndAlso cropRect.Height > 0 Then
            croppedImage = New Bitmap(cropRect.Width, cropRect.Height)
            Using g As Graphics = Graphics.FromImage(croppedImage)
                g.DrawImage(editedImage, New Rectangle(0, 0, cropRect.Width, cropRect.Height), cropRect, GraphicsUnit.Pixel)
            End Using
        End If
    End Sub


    Private Sub Guna2Button1_Click_1(sender As Object, e As EventArgs) Handles Guna2Button1.Click
        PictureBox1.Image = croppedImage
        editedImage = croppedImage
    End Sub
End Class

