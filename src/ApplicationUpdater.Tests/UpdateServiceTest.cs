using System;
using System.Collections.Generic;
using System.Linq;
namespace ApplicationUpdater.Tests
{
	public class UpdateServiceTest
	{
		[Test]
		public void InstallUpdateTest()
		{
			UpdateService updateService = new UpdateService((text, color) =>
			{

			}, (progress) =>{} );

			updateService.InstallUpdate(DateTime.Now, ".\\UpdateExample.zip");

			Assert.Pass();
		}
	}
}
