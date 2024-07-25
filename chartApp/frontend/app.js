// Chart.js grafik nesnesini saklayan global değişken tanımladık ve başlangıç değeri null verdik
let chart = null;

// bu fonksiyonun amacı kullanıcının formda girdiği bilgileri alır, AJAX kullanarak sunucuya gönderir ve sunucudan alınan verileri myChart fonksiyonuyla grafiğe dönüştürür.
function fetchData() {

    // dbType, host, port, dbName, username, password, query, chartType gibi bilgileri kullanıcıdan alınır
	// Formdan alınan parametreleri params dizisine ekler.
    // AJAX isteği ile sunucuya veri gönderir ve gelen yanıtı işleyerek grafiği oluşturur.
    const dbType = $('#dbType').val();
    const host = $('#host').val();
    const port = $('#port').val();
    const dbName = $('#dbName').val();
    const username = $('#username').val();
    const password = $('#password').val();
    const query = $('#query').val();
    const chartType = $('#chartType').val();

    const params = [];
    $('.param-input').each(function () {
        params.push($(this).val());
    });

    if (!dbType || !host || !port || !dbName || !username || !password || !query || !chartType) {
        toastr.error('Lütfen tüm alanları doldurun');
        return;
    }

    const data = {
        dbType: dbType,
        host: host,
        port: port,
        dbName: dbName,
        username: username,
        password: password,
        query: query,
        chartType: chartType,
        parameters: params
    };

    console.log('Gönderilen :', data);

    $.ajax({
        url: 'http://localhost:5000/api/getData',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (response) {
            toastr.success('Veri başarıyla alındı');
            console.log('Response :', response);
            myChart(response, chartType);
        },
        error: function (error) {
            toastr.error('Veri alınırken bir hata oluştu');
            console.error('Error:', error);
        }
    });
}

// Sunucudan alınan verileri kullanarak Chart.js ile grafiği oluşturuyoruz .
function myChart(data, chartType) {
    const ctx = document.getElementById('myChart').getContext('2d');

    if (chart) {
        chart.destroy();
    }

    const datasets = Object.keys(data.values).map((key, index) => ({
        label: key,
        data: data.values[key],
        backgroundColor: `rgba(${Math.floor(Math.random() * 255)}, ${Math.floor(Math.random() * 255)}, ${Math.floor(Math.random() * 255)}, 0.2)`,
        borderColor: `rgba(${Math.floor(Math.random() * 255)}, ${Math.floor(Math.random() * 255)}, ${Math.floor(Math.random() * 255)}, 1)`,
        borderWidth: 1,
    }));

    chart = new Chart(ctx, {
        type: chartType,
        data: {
            labels: data.labels,
            datasets: datasets
        },
        options: {
            scales: {
                y: {
                    beginAtZero: true,
                }
            }
        }
    });

    // kullanıcıya seçilen chart tipini gösteriyoz
    const typeOfChart = document.getElementById("typeOfChart");
    typeOfChart.innerHTML = "Chart Type : " + chartType;
}

// seçtiğimiz view, procedure veya function da bir veya daha parametre varsa formda yeni bir parametre girişi ekliyoruz.
function addParameter() {
    const parameterDiv = document.createElement('div');
    parameterDiv.className = 'form-group';
    parameterDiv.innerHTML = '<input type="text" class="form-control param-input" placeholder="Parametre">';
    document.getElementById('parameters').appendChild(parameterDiv);
}
