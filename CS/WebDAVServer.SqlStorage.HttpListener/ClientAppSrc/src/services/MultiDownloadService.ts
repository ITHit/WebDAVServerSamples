const delay = (milliseconds: number) =>
  new Promise((resolve) => {
    setTimeout(resolve, milliseconds);
  });

const download = async (url: string, name: string) => {
  const a = document.createElement("a");
  a.download = name;
  a.href = url;
  a.style.display = "none";
  document.body.append(a);
  a.click();

  // Chrome requires the timeout
  await delay(100);
  a.remove();
};

export default async function multiDownload(urls: string[]) {
  urls.map(async (url, index) => {
    await delay(index * 1000);
    download(url, "");
  });
}
